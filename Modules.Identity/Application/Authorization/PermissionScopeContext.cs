namespace Modules.System.Identity.Application.Authorization;

public sealed record PermissionScopeContext(
    Guid? BranchId = null,
    bool BranchAccessValidated = false,
    Guid? OwnerUserId = null);
