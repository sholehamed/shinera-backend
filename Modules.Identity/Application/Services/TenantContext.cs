using Application.SharedKernel.Abstractions;
using Modules.System.Identity.Application.Abstractions;

namespace Modules.System.Identity.Application.Services;

public sealed class TenantContext : ITenantContext, ICurrentTenant
{
    private Guid[] _readable = [];
    private Guid[] _writable = [];
    private Guid[] _readableBranches = [];
    private Guid[] _writableBranches = [];
    private int _disableDepth;

    public Guid? UserId { get; private set; }
    public Guid? ActiveTenantId { get; private set; }
    public Guid? ActiveBranchId { get; private set; }

    public Guid? TenantId => ActiveTenantId;
    public Guid? BranchId => ActiveBranchId;

    public bool IsSuperAdmin { get; private set; }

    public IReadOnlyCollection<Guid> ReadableTenantIds => _readable;
    public IReadOnlyCollection<Guid> WritableTenantIds => _writable;
    public IReadOnlyCollection<Guid> ReadableBranchIds => _readableBranches;
    public IReadOnlyCollection<Guid> WritableBranchIds => _writableBranches;

    public bool IsFilterDisabled => _disableDepth > 0;

    public void Initialize(
        Guid userId,
        bool isSuperAdmin,
        Guid[] readable,
        Guid[] writable,
        Guid? activeTenantId,
        Guid[]? readableBranches = null,
        Guid[]? writableBranches = null,
        Guid? activeBranchId = null)
    {
        UserId = userId;
        IsSuperAdmin = isSuperAdmin;
        _readable = readable;
        _writable = writable;
        ActiveTenantId = activeTenantId;
        _readableBranches = readableBranches ?? [];
        _writableBranches = writableBranches ?? [];
        ActiveBranchId = activeBranchId;
    }

    public IDisposable DisableFilter()
    {
        _disableDepth++;
        return new Scope(this);
    }

    private sealed class Scope(TenantContext context) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            context._disableDepth--;
            _disposed = true;
        }
    }
}
