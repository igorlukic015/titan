namespace Titan.Core.models;

public class Result<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }
    public required bool IsSuccess { get; init; }

    public static Result<T> CreateSuccess(T value)
    {
        return new() { Value = value, IsSuccess = true };
    }

    public static Result<T> CreateError(string error)
    {
        return new() { Error = error, IsSuccess = false };
    }
}

