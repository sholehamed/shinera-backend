using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.SharedKernel.Abstractions;

public interface IStaffBookingConcurrencyGuard
{
    Task<bool> TryAcquireAsync(
        DatabaseFacade database,
        Guid tenantId,
        Guid staffId,
        CancellationToken cancellationToken = default);
}
