using Application.SharedKernel.Services;

namespace Application.Tests.Time;

public sealed class TimeZoneResolverTests
{
    private readonly TimeZoneResolver _resolver = new();

    [Fact]
    public void ResolveToUtc_ValidIanaZone_RoundTripsBusinessTime()
    {
        var result = _resolver.ResolveToUtc(
            new DateOnly(2026, 10, 10),
            new TimeOnly(9, 0),
            "Asia/Tehran");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new DateTimeOffset(
                2026, 10, 10, 5, 30, 0, TimeSpan.Zero),
            result.Value);

        var roundTrip = _resolver.ResolveFromUtc(
            result.Value,
            "Asia/Tehran");

        Assert.True(roundTrip.IsSuccess);
        Assert.Equal(
            new DateOnly(2026, 10, 10),
            roundTrip.Value.Date);
        Assert.Equal(
            new TimeOnly(9, 0),
            roundTrip.Value.Time);
    }

    [Fact]
    public void ResolveToUtc_InvalidDstGap_IsRejected()
    {
        var result = _resolver.ResolveToUtc(
            new DateOnly(2026, 3, 29),
            new TimeOnly(2, 30),
            "Europe/Amsterdam");

        Assert.True(result.IsFailure);
        Assert.Equal(
            "time.invalid_local_time",
            result.Error.Code);
    }

    [Fact]
    public void ResolveToUtc_AmbiguousDstTime_IsRejected()
    {
        var result = _resolver.ResolveToUtc(
            new DateOnly(2026, 10, 25),
            new TimeOnly(2, 30),
            "Europe/Amsterdam");

        Assert.True(result.IsFailure);
        Assert.Equal(
            "time.ambiguous_local_time",
            result.Error.Code);
    }

    [Theory]
    [InlineData("+03:30")]
    [InlineData("Invalid/Zone")]
    [InlineData("")]
    public void FixedOffsetOrUnknownZone_IsRejected(
        string timeZoneId)
    {
        Assert.False(
            _resolver.IsValidIanaTimeZoneId(timeZoneId));
    }
}
