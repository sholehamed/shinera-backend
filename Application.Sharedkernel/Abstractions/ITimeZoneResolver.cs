using Application.SharedKernel.Models;

namespace Application.SharedKernel.Abstractions;

public sealed record BusinessLocalTime(
    DateOnly Date,
    TimeOnly Time);

public interface ITimeZoneResolver
{
    bool IsValidIanaTimeZoneId(string timeZoneId);

    Result<DateTimeOffset> ResolveToUtc(
        DateOnly businessDate,
        TimeOnly localTime,
        string timeZoneId);

    Result<BusinessLocalTime> ResolveFromUtc(
        DateTimeOffset utcInstant,
        string timeZoneId);
}
