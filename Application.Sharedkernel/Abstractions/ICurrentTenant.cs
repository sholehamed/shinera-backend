namespace Application.SharedKernel.Abstractions;

public interface ICurrentTenant
{
    Guid? TenantId { get; }
    Guid? BranchId { get; }

    IReadOnlyCollection<Guid> WritableTenantIds { get; }
    IReadOnlyCollection<Guid> WritableBranchIds { get; }

    bool IsFilterDisabled { get; }
}
