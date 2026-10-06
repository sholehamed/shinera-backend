using Application.SharedKernel.Abstractions;
using Application.SharedKernel.Models;

namespace Application.SharedKernel.Services;

public sealed class TimeZoneResolver : ITimeZoneResolver
{
    public bool IsValidIanaTimeZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        var normalized = timeZoneId.Trim();

        if (!normalized.Contains('/', StringComparison.Ordinal))
            return false;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(normalized);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    public Result<DateTimeOffset> ResolveToUtc(
        DateOnly businessDate,
        TimeOnly localTime,
        string timeZoneId)
    {
        if (!IsValidIanaTimeZoneId(timeZoneId))
        {
            return Result<DateTimeOffset>.Failure(
                Error.Validation(
                    "time.invalid_timezone",
                    "The configured time zone is not a valid IANA time zone identifier."));
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(
            timeZoneId.Trim());

        var localDateTime = businessDate.ToDateTime(
            localTime,
            DateTimeKind.Unspecified);

        if (zone.IsInvalidTime(localDateTime))
        {
            return Result<DateTimeOffset>.Failure(
                Error.Validation(
                    "time.invalid_local_time",
                    "The selected local time does not exist in the configured branch time zone."));
        }

        if (zone.IsAmbiguousTime(localDateTime))
        {
            return Result<DateTimeOffset>.Failure(
                Error.Validation(
                    "time.ambiguous_local_time",
                    "The selected local time is ambiguous in the configured branch time zone."));
        }

        var utc = TimeZoneInfo.ConvertTimeToUtc(
            localDateTime,
            zone);

        return Result<DateTimeOffset>.Success(
            new DateTimeOffset(
                DateTime.SpecifyKind(utc, DateTimeKind.Utc)));
    }
}
