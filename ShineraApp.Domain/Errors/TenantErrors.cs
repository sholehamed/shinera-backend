namespace ShineraApp.Domain.Errors
{
    public static class TenantErrors
    {
        public static Error NotFound(Guid tenantId) =>
            Error.NotFound(
                "Tenant.NotFound",
                $"Tenant with id '{tenantId}' was not found.");
    }
}
