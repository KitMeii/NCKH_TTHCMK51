namespace Shared.Infrastructure.Common;

public sealed class ErrorInfo
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Response envelope every service endpoint must return: { success, data, error }.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ErrorInfo? Error { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResponse<T> Fail(string code, string message) =>
        new() { Success = false, Error = new ErrorInfo { Code = code, Message = message } };
}

/// <summary>Non-generic helper for endpoints with no payload (e.g. 204-style success, or error-only responses).</summary>
public static class ApiResponse
{
    public static ApiResponse<object?> Ok() => ApiResponse<object?>.Ok(null);

    public static ApiResponse<object?> Fail(string code, string message) =>
        ApiResponse<object?>.Fail(code, message);
}
