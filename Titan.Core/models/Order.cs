using Titan.Core.Enums;

namespace Titan.Core.Models;

public class Order
{
    public required Guid Id { get; init; }
    public required string Symbol { get; init; }
    public required decimal Price { get; init; }
    public required decimal Quantity { get; init; }
    public required OrderType Type { get; init; }
    public required OrderSide Side { get; init; }
    public OrderStatus Status { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTime Timestamp { get; init; }
}
