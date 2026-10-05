namespace Modules.System.Identity.Application.Authorization;

public interface IPermissionAuthorizationService
{
    Task<PermissionDecision> AuthorizeAsync(
        Guid userId,
        string resource,
        string action,
        PermissionScopeContext? resourceContext = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EffectivePermissionDto>> GetEffectivePermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record EffectivePermissionDto(
    string Key,
    string Scope,
    Guid? ScopeReferenceId);
