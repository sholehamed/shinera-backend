using Domain.SharedKernel.Entities;
using Domain.SharedKernel.Common;
namespace ShineraApp.Domain.Entities;

public sealed class BusinessProfile : FullAuditableEntity, IMustHaveTenant
{
    private BusinessProfile() { }
    public BusinessProfile(Guid tenantId, string activityType, string phone, string city, string address,
        string postalCode, string instagram)
    {
        TenantId = tenantId; ActivityType = activityType; Phone = phone;
        City = city; Address = address; PostalCode = postalCode; Instagram = instagram;
    }
    public Guid TenantId { get; private set; }
    public string ActivityType { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string City { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public string Instagram { get; private set; } = "";
}
