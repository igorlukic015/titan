namespace Titan.Simulator.DTOs;

public record SubmitOrderRequest
{
    public required string Symbol { get; init; }
    public required decimal Price { get; init; }
    public required decimal Quantity { get; init; }
    public required string Type { get; init; }
    public required string Side { get; init; }
}
