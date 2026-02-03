using Microsoft.Extensions.Logging;
using Titan.Core.Enums;
using Titan.Core.Models;
using Titan.Engine.Interfaces;

namespace Titan.Engine.services;

public class OrderBook : IOrderBook
{
    private readonly List<Order> bids;
    private readonly List<Order> asks;
    private readonly Lock lockObject;
    private readonly ILogger<OrderBook> logger;

    public string Symbol { get; }

    public OrderBook(string symbol, ILogger<OrderBook> logger)
    {
        Symbol = symbol;
        this.logger = logger;
        bids = [];
        asks = [];
        lockObject = new Lock();
    }

    public IReadOnlyList<Trade> ProcessOrder(Order order)
    {
        lock (lockObject)
        {
            List<Trade> trades = [];

            if (order.Symbol != Symbol)
            {
                throw new ArgumentException($"Order symbol {order.Symbol} does not match OrderBook symbol {Symbol}");
            }

            List<Order> oppositeList = order.Side == OrderSide.Buy ? asks : bids;
            List<Order> ownList = order.Side == OrderSide.Buy ? bids : asks;

            logger.LogInformation("Order {OrderId} {Side} {Qty}@{Price} attempting match on {Symbol}",
                order.Id, order.Side, order.Quantity, order.Price, Symbol);

            while (order.RemainingQuantity > 0 && oppositeList.Count > 0)
            {
                Order topOrder = oppositeList[0];

                if (!CanMatch(order, topOrder))
                {
                    logger.LogDebug("Order {OrderId} cannot match against {BookOrderId} ({Price} vs {BookPrice})",
                        order.Id, topOrder.Id, order.Price, topOrder.Price);
                    break;
                }

                decimal matchedQuantity = Math.Min(order.RemainingQuantity, topOrder.RemainingQuantity);

                Trade trade = CreateTrade(order, topOrder, matchedQuantity);
                trades.Add(trade);

                order.RemainingQuantity -= matchedQuantity;
                topOrder.RemainingQuantity -= matchedQuantity;

                UpdateOrderStatus(order);
                UpdateOrderStatus(topOrder);

                logger.LogInformation("Trade {TradeId}: Order {IncomingId} matched {BookOrderId}, {MatchedQty}@{TradePrice}",
                    trade.Id, order.Id, topOrder.Id, matchedQuantity, trade.Price);

                if (topOrder.RemainingQuantity == 0)
                {
                    oppositeList.RemoveAt(0);
                }
            }

            if (order.RemainingQuantity > 0)
            {
                logger.LogInformation("Order {OrderId} resting on book, remaining {RemainingQty}",
                    order.Id, order.RemainingQuantity);
                ownList.Add(order);
                SortOrders(ownList, order.Side);
            }

            return trades.AsReadOnly();
        }
    }

    public IReadOnlyList<Order> GetBids()
    {
        lock (lockObject)
        {
            return bids.ToList().AsReadOnly();
        }
    }

    public IReadOnlyList<Order> GetAsks()
    {
        lock (lockObject)
        {
            return asks.ToList().AsReadOnly();
        }
    }

    private bool CanMatch(Order incomingOrder, Order bookOrder)
    {
        return incomingOrder.Side == OrderSide.Buy
            ? incomingOrder.Price >= bookOrder.Price
            : incomingOrder.Price <= bookOrder.Price;
    }

    private Trade CreateTrade(Order incomingOrder, Order bookOrder, decimal quantity)
    {
        decimal tradePrice = bookOrder.Price;

        TradeType tradeType = bookOrder.Side == OrderSide.Buy
            ? TradeType.MakerBuy
            : TradeType.MakerSell;

        return new Trade
        {
            Id = Guid.NewGuid(),
            BuyOrderId = incomingOrder.Side == OrderSide.Buy ? incomingOrder.Id : bookOrder.Id,
            SellOrderId = incomingOrder.Side == OrderSide.Sell ? incomingOrder.Id : bookOrder.Id,
            Symbol = Symbol,
            Price = tradePrice,
            Quantity = quantity,
            Timestamp = DateTime.UtcNow,
            Type = tradeType
        };
    }

    private static void UpdateOrderStatus(Order order)
    {
        if (order.RemainingQuantity == 0)
        {
            order.Status = OrderStatus.Filled;
        }
        else if (order.Quantity > order.RemainingQuantity)
        {
            order.Status = OrderStatus.PartiallyFilled;
        }
    }

    private static void SortOrders(List<Order> orders, OrderSide side)
    {
        if (side == OrderSide.Buy)
        {
            orders.Sort((a, b) =>
            {
                int priceCompare = b.Price.CompareTo(a.Price);
                return priceCompare != 0 ? priceCompare : a.Timestamp.CompareTo(b.Timestamp);
            });
        }
        else
        {
            orders.Sort((a, b) =>
            {
                int priceCompare = a.Price.CompareTo(b.Price);
                return priceCompare != 0 ? priceCompare : a.Timestamp.CompareTo(b.Timestamp);
            });
        }
    }
}