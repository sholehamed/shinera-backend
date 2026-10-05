namespace Domain.Sharedkernel.Util;

[Obsolete("Inject System.TimeProvider. This compatibility shim will be removed after M0.1.")]
public static class TimeProvider
{
    public static DateTimeOffset UtcNowOffset =>
        global::System.TimeProvider.System.GetUtcNow();

    public static DateTime UtcNow =>
        UtcNowOffset.UtcDateTime;

    [Obsolete("Server-local time is not authoritative. Use UtcNowOffset.")]
    public static DateTime Now =>
        UtcNow;

    [Obsolete("Server-local time is not authoritative. Use UtcNowOffset.")]
    public static DateTimeOffset NowOffset =>
        UtcNowOffset;
}
