namespace Titan.Gateway.DTOs;

public record TradeResponse
{
    public required Guid TradeId { get; init; }
    public required Guid BuyOrderId { get; init; }
    public required Guid SellOrderId { get; init; }
    public required string Symbol { get; init; }
    public required decimal Price { get; init; }
    public required decimal Quantity { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Type { get; init; }
}
