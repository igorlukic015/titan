using Microsoft.AspNetCore.Http.HttpResults;
using Titan.Core.Enums;
using Titan.Core.Models;
using Titan.Engine.Interfaces;
using Titan.Gateway.DTOs;

namespace Titan.Gateway.Endpoints;

public static class OrderEndpoints
{
    public static Results<Ok<SubmitOrderResponse>, BadRequest<ErrorResponse>> SubmitOrder(
        SubmitOrderRequest request,
        IOrderBook orderBook)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Symbol is required" });
        }

        if (request.Price <= 0)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Price must be greater than 0" });
        }

        if (request.Quantity <= 0)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Quantity must be greater than 0" });
        }

        if (!Enum.TryParse<OrderType>(request.Type, ignoreCase: true, out OrderType orderType))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Invalid order type. Must be 'Limit' or 'Market'" });
        }

        if (!Enum.TryParse<OrderSide>(request.Side, ignoreCase: true, out OrderSide orderSide))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Invalid order side. Must be 'Buy' or 'Sell'" });
        }

        Order order = new Order
        {
            Id = Guid.NewGuid(),
            Symbol = request.Symbol,
            Price = request.Price,
            Quantity = request.Quantity,
            Type = orderType,
            Side = orderSide,
            Status = OrderStatus.Pending,
            RemainingQuantity = request.Quantity,
            Timestamp = DateTime.UtcNow
        };

        IReadOnlyList<Trade> trades;
        try
        {
            trades = orderBook.ProcessOrder(order);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }

        SubmitOrderResponse response = new SubmitOrderResponse
        {
            OrderId = order.Id,
            Symbol = order.Symbol,
            Status = order.Status.ToString(),
            RemainingQuantity = order.RemainingQuantity,
            Trades = trades.Select(t => new TradeResponse
            {
                TradeId = t.Id,
                BuyOrderId = t.BuyOrderId,
                SellOrderId = t.SellOrderId,
                Symbol = t.Symbol,
                Price = t.Price,
                Quantity = t.Quantity,
                Timestamp = t.Timestamp,
                Type = t.Type.ToString()
            }).ToList()
        };

        return TypedResults.Ok(response);
    }
}
