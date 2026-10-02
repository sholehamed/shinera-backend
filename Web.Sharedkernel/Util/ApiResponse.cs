namespace Web.Sharedkernel.Util
{
    public sealed record ApiResponse(
    bool Success,
    ApiError? Error)
    {
        public static ApiResponse Ok() =>
            new(true, null);

        public static ApiResponse Fail(
            string code,
            string message) =>
            new(
                false,
                new ApiError(code, message));
    }
    public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    ApiError? Error)
    {
        public static ApiResponse<T> Ok(T data) =>
            new(true, data, null);

        public static ApiResponse<T> Fail(
            string code,
            string message) =>
            new(
                false,
                default,
                new ApiError(code, message));
    }

    public sealed record ApiError(
        string Code,
        string Message);
}
