using Application.SharedKernel.Models;

namespace Application.SharedKernel.Abstractions;

public interface ITimeZoneResolver
{
    bool IsValidIanaTimeZoneId(string timeZoneId);

    Result<DateTimeOffset> ResolveToUtc(
        DateOnly businessDate,
        TimeOnly localTime,
        string timeZoneId);
}
