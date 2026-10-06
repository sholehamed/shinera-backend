using Application.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Tests.TestDoubles;

internal sealed class AllowStaffBookingConcurrencyGuard
    : IStaffBookingConcurrencyGuard
{
    public Task<bool> TryAcquireAsync(
        DatabaseFacade database,
        Guid tenantId,
        Guid staffId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
