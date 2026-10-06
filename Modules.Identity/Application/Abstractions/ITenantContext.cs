namespace Modules.System.Identity.Application.Abstractions;

public interface ITenantContext
{
    Guid? UserId { get; }
    Guid? ActiveTenantId { get; }
    bool IsSuperAdmin { get; }

    IReadOnlyCollection<Guid> ReadableTenantIds { get; }
    IReadOnlyCollection<Guid> WritableTenantIds { get; }

    bool IsFilterDisabled { get; }

    IDisposable DisableFilter();

    void Initialize(
        Guid userId,
        bool isSuperAdmin,
        Guid[] readable,
        Guid[] writable,
        Guid? activeTenantId);
}
