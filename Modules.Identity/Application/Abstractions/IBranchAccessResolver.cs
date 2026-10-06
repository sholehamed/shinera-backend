namespace Modules.System.Identity.Application.Abstractions;

public interface IBranchAccessResolver
{
    Task<(Guid[] Readable, Guid[] Writable)> ResolveAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken);
}
