using Titan.Core.Enums;
using Titan.Core.models;
using Titan.Core.Models;
using Titan.Engine.interfaces;
using Titan.Engine.Interfaces;

namespace Titan.Engine.services;

public class OrderService : IOrderService
{
    private readonly IOrderBook orderBook;

    public OrderService(IOrderBook orderBook)
    {
        this.orderBook = orderBook;
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
        try
        {
            IReadOnlyList<Trade> trades = orderBook.ProcessOrder(order);
            return Result<IReadOnlyList<Trade>>.CreateSuccess(trades);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<Trade>>.CreateError(ex.Message);
        }
    }
}
