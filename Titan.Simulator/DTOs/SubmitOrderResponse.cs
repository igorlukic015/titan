namespace Titan.Simulator.DTOs;

public record SubmitOrderResponse
{
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
}
