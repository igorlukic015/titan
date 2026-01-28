using Microsoft.AspNetCore.Http.HttpResults;
using Titan.Core.models;
using Titan.Core.Models;
using Titan.Engine.interfaces;
using Titan.Gateway.DTOs;

namespace Titan.Gateway.Endpoints;

public static class OrderEndpoints
{
    public static Results<Ok<SubmitOrderResponse>, BadRequest<ErrorResponse>> SubmitOrder(
        SubmitOrderRequest request,
        IOrderService orderService)
    {
        Result<Order> orderOrError =
            orderService.CreateOrder(request.Symbol, request.Price, request.Quantity, request.Type, request.Side);

        if (!orderOrError.IsSuccess)
        {
            return TypedResults.BadRequest(new ErrorResponse() { Error = orderOrError.Error });
        }

        Result<IReadOnlyList<Trade>> tradesOrError = orderService.CreateTrade(orderOrError.Value);

        SubmitOrderResponse response = new()
        {
            OrderId = orderOrError.Value.Id,
            Symbol = orderOrError.Value.Symbol,
            Status = orderOrError.Value.Status.ToString(),
            RemainingQuantity = orderOrError.Value.RemainingQuantity,
            Trades = [.. tradesOrError.Value.Select(t => new TradeResponse
            {
                TradeId = t.Id,
                BuyOrderId = t.BuyOrderId,
                SellOrderId = t.SellOrderId,
                Symbol = t.Symbol,
                Price = t.Price,
                Quantity = t.Quantity,
                Timestamp = t.Timestamp,
                Type = t.Type.ToString()
            })]
        };

        return TypedResults.Ok(response);
    }
}
