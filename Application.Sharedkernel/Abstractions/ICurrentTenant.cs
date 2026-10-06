namespace Application.SharedKernel.Abstractions;

public interface ICurrentTenant
{
    Guid? TenantId { get; }
    IReadOnlyCollection<Guid> WritableTenantIds { get; }
    bool IsFilterDisabled { get; }
}
