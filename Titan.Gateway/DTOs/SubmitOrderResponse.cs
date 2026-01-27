namespace Titan.Gateway.DTOs;

public record SubmitOrderResponse
{
    public required Guid OrderId { get; init; }
    public required string Symbol { get; init; }
    public required string Status { get; init; }
    public required decimal RemainingQuantity { get; init; }
    public required List<TradeResponse> Trades { get; init; }
}
