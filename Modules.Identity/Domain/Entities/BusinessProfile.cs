using Domain.SharedKernel.Common;
using Domain.SharedKernel.Entities;

namespace Modules.System.Identity.Domain.Entities;

public enum BusinessMode
{
    Solo = 1,
    Salon = 2
}

public sealed class BusinessProfile : AuditableEntity, IMustHaveTenant
{
    public BusinessProfile() : base(25)
    {
    }

    public BusinessProfile(
        Guid tenantId,
        string displayName,
        string businessType,
        BusinessMode mode,
        string? phone = null,
        string? email = null,
        string? city = null,
        string? address = null,
        string? description = null,
        Guid? logoId = null) : base(25)
    {
        TenantId = tenantId;
        DisplayName = displayName.Trim();
        BusinessType = businessType.Trim();
        Mode = mode;
        Phone = Normalize(phone);
        Email = Normalize(email);
        City = Normalize(city);
        Address = Normalize(address);
        Description = Normalize(description);
        LogoId = logoId;
    }

    public Guid TenantId { get; set; }
    public string DisplayName { get; set; } = default!;
    public string BusinessType { get; set; } = default!;
    public BusinessMode Mode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public Guid? LogoId { get; set; }
    public string? Description { get; set; }

    public Tenant Tenant { get; set; } = default!;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
