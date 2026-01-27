namespace Titan.Gateway.DTOs;

public record ErrorResponse
{
    public required string Error { get; init; }
}
