namespace Web.SharedKernel.Models;

public sealed record ApiError(string Code, string Message, object? Details = null);

public sealed record ApiResponse(bool Success, ApiError? Error)
{
    public static ApiResponse Ok() => new(true, null);

    public static ApiResponse Fail(string code, string message, object? details = null) =>
        new(false, new ApiError(code, message, details));
}

public sealed record ApiResponse<T>(bool Success, T? Data, ApiError? Error)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null);

    public static ApiResponse<T> Fail(string code, string message, object? details = null) =>
        new(false, default, new ApiError(code, message, details));
}
