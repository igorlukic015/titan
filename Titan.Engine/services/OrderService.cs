using Microsoft.Extensions.Logging;
using Titan.Core.Enums;
using Titan.Core.models;
using Titan.Core.Models;
using Titan.Engine.Data;
using Titan.Engine.interfaces;
using Titan.Engine.Interfaces;

namespace Titan.Engine.services;

public class OrderService : IOrderService
{
    private readonly IOrderBook orderBook;
    private readonly ILogger<OrderService> logger;
    private readonly TradeDbContext dbContext;

    public OrderService(IOrderBook orderBook, ILogger<OrderService> logger, TradeDbContext dbContext)
    {
        this.orderBook = orderBook;
        this.logger = logger;
        this.dbContext = dbContext;
    }

    public Result<Order> CreateOrder(string symbol, decimal price, decimal quantity, string type, string side)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return Result<Order>.CreateError("Symbol is required");
        }

        if (price <= 0)
        {
            return Result<Order>.CreateError("Price must be greater than 0");
        }

        if (quantity <= 0)
        {
            return Result<Order>.CreateError("Quantity must be greater than 0");
        }

        if (!Enum.TryParse(type, ignoreCase: true, out OrderType orderType))
        {
            return Result<Order>.CreateError("Invalid order type. Must be 'Limit' or 'Market'");
        }

        if (!Enum.TryParse(side, ignoreCase: true, out OrderSide orderSide))
        {
            return Result<Order>.CreateError("Invalid order side. Must be 'Buy' or 'Sell'");
        }

        Order order = new()
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Price = price,
            Quantity = quantity,
            Type = orderType,
            Side = orderSide,
            Status = OrderStatus.Pending,
            RemainingQuantity = quantity,
            Timestamp = DateTime.UtcNow
        };

        return Result<Order>.CreateSuccess(order);
    }

    public Result<IReadOnlyList<Trade>> CreateTrade(Order order)
    {
        logger.LogInformation("Submitting order {OrderId} to book for matching", order.Id);

        try
        {
            IReadOnlyList<Trade> trades = orderBook.ProcessOrder(order);
            logger.LogInformation("Order {OrderId} produced {TradeCount} trade(s)", order.Id, trades.Count);

            foreach (Trade trade in trades)
            {
                dbContext.Trades.Add(new TradeEntity
                {
                    Id = trade.Id,
                    BuyOrderId = trade.BuyOrderId,
                    SellOrderId = trade.SellOrderId,
                    Symbol = trade.Symbol,
                    Price = trade.Price,
                    Quantity = trade.Quantity,
                    Timestamp = trade.Timestamp,
                    Type = trade.Type
                });
            }

            if (trades.Count > 0)
            {
                dbContext.SaveChanges();
            }

            return Result<IReadOnlyList<Trade>>.CreateSuccess(trades);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Order {OrderId} failed during matching", order.Id);
            return Result<IReadOnlyList<Trade>>.CreateError(ex.Message);
        }
    }
}
