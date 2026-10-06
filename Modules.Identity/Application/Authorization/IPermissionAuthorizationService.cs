namespace Modules.System.Identity.Application.Authorization;

public interface IPermissionAuthorizationService
{
    Task<PermissionDecision> HasPermissionAsync(
        Guid userId,
        string resource,
        string action,
        CancellationToken cancellationToken = default);

    Task<PermissionDecision> AuthorizeAsync(
        Guid userId,
        string resource,
        string action,
        PermissionScopeContext resourceContext,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EffectivePermissionDto>> GetGrantedScopesAsync(
        Guid userId,
        string resource,
        string action,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EffectivePermissionDto>> GetEffectivePermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record EffectivePermissionDto(
    string Key,
    string Scope,
    Guid? ScopeReferenceId);
