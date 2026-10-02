namespace Application.Sharedkernel.Abstractions
{
    public interface ICurrentTenant
    {
        Guid TenantId { get; }
    }
}
