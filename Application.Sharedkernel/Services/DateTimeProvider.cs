using Application.SharedKernel.Abstractions;

namespace Application.SharedKernel.Services;

[Obsolete("Inject System.TimeProvider directly in new code.")]
public sealed class DateTimeProvider(System.TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
