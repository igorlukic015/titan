using Titan.Core.Enums;
using Titan.Core.Models;
using Titan.Engine.Services;
using Xunit;

namespace Titan.Engine.Tests.Services;

public class OrderBookTests
{
    private readonly string symbol = "BTCUSD";

    [Fact]
    public void Constructor_InitializesWithSymbol()
    {
        OrderBook orderBook = new OrderBook(symbol);

        Assert.Equal(symbol, orderBook.Symbol);
        Assert.Empty(orderBook.GetBids());
        Assert.Empty(orderBook.GetAsks());
    }

    [Fact]
    public void ProcessOrder_RejectsOrderWithDifferentSymbol()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order order = CreateOrder(OrderSide.Buy, 100m, 1m, "ETHUSD");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => orderBook.ProcessOrder(order));
        Assert.Contains("does not match", exception.Message);
    }

    [Fact]
    public void ProcessOrder_BuyOrderWithNoAsks_AddsToBook()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, 1m);

        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(buyOrder);

        Assert.Empty(trades);
        Assert.Single(orderBook.GetBids());
        Assert.Empty(orderBook.GetAsks());
        Assert.Equal(1m, orderBook.GetBids()[0].RemainingQuantity);
    }

    [Fact]
    public void ProcessOrder_SellOrderWithNoBids_AddsToBook()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100m, 1m);

        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(sellOrder);

        Assert.Empty(trades);
        Assert.Empty(orderBook.GetBids());
        Assert.Single(orderBook.GetAsks());
        Assert.Equal(1m, orderBook.GetAsks()[0].RemainingQuantity);
    }

    [Fact]
    public void ProcessOrder_BuyMatchesAsk_ReturnsOneTrade()
    {
        OrderBook orderBook = new OrderBook(symbol);
        decimal askPrice = 100m;
        decimal quantity = 1m;
        Order sellOrder = CreateOrder(OrderSide.Sell, askPrice, quantity);
        orderBook.ProcessOrder(sellOrder);

        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, quantity);
        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(buyOrder);

        Assert.Single(trades);
        Assert.Equal(askPrice, trades[0].Price);
        Assert.Equal(quantity, trades[0].Quantity);
        Assert.Empty(orderBook.GetBids());
        Assert.Empty(orderBook.GetAsks());
    }

    [Fact]
    public void ProcessOrder_SellMatchesBid_ReturnsOneTrade()
    {
        OrderBook orderBook = new OrderBook(symbol);
        decimal bidPrice = 100m;
        decimal quantity = 1m;
        Order buyOrder = CreateOrder(OrderSide.Buy, bidPrice, quantity);
        orderBook.ProcessOrder(buyOrder);

        Order sellOrder = CreateOrder(OrderSide.Sell, 100m, quantity);
        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(sellOrder);

        Assert.Single(trades);
        Assert.Equal(bidPrice, trades[0].Price);
        Assert.Equal(quantity, trades[0].Quantity);
        Assert.Empty(orderBook.GetBids());
        Assert.Empty(orderBook.GetAsks());
    }

    [Fact]
    public void ProcessOrder_PartialFill_UpdatesRemainingQuantity()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100m, 2m);
        orderBook.ProcessOrder(sellOrder);

        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, 5m);
        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(buyOrder);

        Assert.Single(trades);
        Assert.Equal(2m, trades[0].Quantity);
        Assert.Single(orderBook.GetBids());
        Assert.Equal(3m, orderBook.GetBids()[0].RemainingQuantity);
        Assert.Empty(orderBook.GetAsks());
    }

    [Fact]
    public void ProcessOrder_MultipleMatches_ReturnsMultipleTrades()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order sell1 = CreateOrder(OrderSide.Sell, 100m, 1m, timestamp: DateTime.UtcNow.AddSeconds(-2));
        Order sell2 = CreateOrder(OrderSide.Sell, 100m, 1m, timestamp: DateTime.UtcNow.AddSeconds(-1));
        orderBook.ProcessOrder(sell1);
        orderBook.ProcessOrder(sell2);

        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, 2m);
        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(buyOrder);

        Assert.Equal(2, trades.Count);
        Assert.Equal(1m, trades[0].Quantity);
        Assert.Equal(1m, trades[1].Quantity);
        Assert.Empty(orderBook.GetBids());
        Assert.Empty(orderBook.GetAsks());
    }

    [Fact]
    public void ProcessOrder_NoMatch_BuyPriceTooLow()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100m, 1m);
        orderBook.ProcessOrder(sellOrder);

        Order buyOrder = CreateOrder(OrderSide.Buy, 99m, 1m);
        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(buyOrder);

        Assert.Empty(trades);
        Assert.Single(orderBook.GetBids());
        Assert.Single(orderBook.GetAsks());
    }

    [Fact]
    public void ProcessOrder_NoMatch_SellPriceTooHigh()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, 1m);
        orderBook.ProcessOrder(buyOrder);

        Order sellOrder = CreateOrder(OrderSide.Sell, 101m, 1m);
        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(sellOrder);

        Assert.Empty(trades);
        Assert.Single(orderBook.GetBids());
        Assert.Single(orderBook.GetAsks());
    }

    [Fact]
    public void ProcessOrder_BidsOrderedByPriceThenTime()
    {
        OrderBook orderBook = new OrderBook(symbol);
        DateTime baseTime = DateTime.UtcNow;
        Order buy1 = CreateOrder(OrderSide.Buy, 100m, 1m, timestamp: baseTime.AddSeconds(-3));
        Order buy2 = CreateOrder(OrderSide.Buy, 101m, 1m, timestamp: baseTime.AddSeconds(-2));
        Order buy3 = CreateOrder(OrderSide.Buy, 100m, 1m, timestamp: baseTime.AddSeconds(-1));

        orderBook.ProcessOrder(buy1);
        orderBook.ProcessOrder(buy2);
        orderBook.ProcessOrder(buy3);

        IReadOnlyList<Order> bids = orderBook.GetBids();
        Assert.Equal(3, bids.Count);
        Assert.Equal(101m, bids[0].Price);
        Assert.Equal(100m, bids[1].Price);
        Assert.Equal(100m, bids[2].Price);
        Assert.True(bids[1].Timestamp < bids[2].Timestamp);
    }

    [Fact]
    public void ProcessOrder_AsksOrderedByPriceThenTime()
    {
        OrderBook orderBook = new OrderBook(symbol);
        DateTime baseTime = DateTime.UtcNow;
        Order sell1 = CreateOrder(OrderSide.Sell, 100m, 1m, timestamp: baseTime.AddSeconds(-3));
        Order sell2 = CreateOrder(OrderSide.Sell, 99m, 1m, timestamp: baseTime.AddSeconds(-2));
        Order sell3 = CreateOrder(OrderSide.Sell, 100m, 1m, timestamp: baseTime.AddSeconds(-1));

        orderBook.ProcessOrder(sell1);
        orderBook.ProcessOrder(sell2);
        orderBook.ProcessOrder(sell3);

        IReadOnlyList<Order> asks = orderBook.GetAsks();
        Assert.Equal(3, asks.Count);
        Assert.Equal(99m, asks[0].Price);
        Assert.Equal(100m, asks[1].Price);
        Assert.Equal(100m, asks[2].Price);
        Assert.True(asks[1].Timestamp < asks[2].Timestamp);
    }

    [Fact]
    public void ProcessOrder_TradeExecutesAtMakerPrice()
    {
        OrderBook orderBook = new OrderBook(symbol);
        decimal makerPrice = 100m;
        Order sellOrder = CreateOrder(OrderSide.Sell, makerPrice, 1m);
        orderBook.ProcessOrder(sellOrder);

        Order buyOrder = CreateOrder(OrderSide.Buy, 105m, 1m);
        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(buyOrder);

        Assert.Single(trades);
        Assert.Equal(makerPrice, trades[0].Price);
    }

    [Fact]
    public void ProcessOrder_UpdatesOrderStatus_Filled()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100m, 1m);
        orderBook.ProcessOrder(sellOrder);

        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, 1m);
        orderBook.ProcessOrder(buyOrder);

        Assert.Equal(OrderStatus.Filled, buyOrder.Status);
        Assert.Equal(OrderStatus.Filled, sellOrder.Status);
    }

    [Fact]
    public void ProcessOrder_UpdatesOrderStatus_PartiallyFilled()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100m, 2m);
        orderBook.ProcessOrder(sellOrder);

        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, 5m);
        orderBook.ProcessOrder(buyOrder);

        Assert.Equal(OrderStatus.Filled, sellOrder.Status);
        Assert.Equal(OrderStatus.PartiallyFilled, buyOrder.Status);
    }

    [Fact]
    public void GetBids_ReturnsReadOnlyCopy()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, 1m);
        orderBook.ProcessOrder(buyOrder);

        IReadOnlyList<Order> bids1 = orderBook.GetBids();
        IReadOnlyList<Order> bids2 = orderBook.GetBids();

        Assert.NotSame(bids1, bids2);
    }

    [Fact]
    public void ProcessOrder_TradeRecordsCorrectOrderIds()
    {
        OrderBook orderBook = new OrderBook(symbol);
        Order sellOrder = CreateOrder(OrderSide.Sell, 100m, 1m);
        orderBook.ProcessOrder(sellOrder);

        Order buyOrder = CreateOrder(OrderSide.Buy, 100m, 1m);
        IReadOnlyList<Trade> trades = orderBook.ProcessOrder(buyOrder);

        Assert.Single(trades);
        Assert.Equal(buyOrder.Id, trades[0].BuyOrderId);
        Assert.Equal(sellOrder.Id, trades[0].SellOrderId);
    }

    private Order CreateOrder(OrderSide side, decimal price, decimal quantity, string? symbol = null, DateTime? timestamp = null)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Symbol = symbol ?? this.symbol,
            Price = price,
            Quantity = quantity,
            Type = OrderType.Limit,
            Side = side,
            Status = OrderStatus.Pending,
            RemainingQuantity = quantity,
            Timestamp = timestamp ?? DateTime.UtcNow
        };
    }
}
