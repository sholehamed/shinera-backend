using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Authorization;

public enum PermissionDecisionCode
{
    Allowed = 0,
    TenantContextMissing = 1,
    TenantMembershipRequired = 2,
    PermissionRequired = 3,
    ScopeDenied = 4,
    UserInactive = 5
}

public sealed record PermissionDecision(
    bool IsAllowed,
    PermissionDecisionCode Code,
    string PermissionKey,
    PermissionScopeType? MatchedScope = null)
{
    public static PermissionDecision Allow(
        string permissionKey,
        PermissionScopeType scope) =>
        new(true, PermissionDecisionCode.Allowed, permissionKey, scope);

    public static PermissionDecision Deny(
        string permissionKey,
        PermissionDecisionCode code) =>
        new(false, code, permissionKey);
}
