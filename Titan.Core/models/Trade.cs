using Titan.Core.Enums;

namespace Titan.Core.Models;

public record Trade
{
    public required Guid Id { get; init; }
    public required Guid BuyOrderId { get; init; }
    public required Guid SellOrderId { get; init; }
    public required string Symbol { get; init; }
    public required decimal Price { get; init; }
    public required decimal Quantity { get; init; }
    public required DateTime Timestamp { get; init; }
    public required TradeType Type { get; init; }
}
