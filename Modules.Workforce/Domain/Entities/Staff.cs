namespace Modules.System.Workforce.Domain.Entities;

public sealed class Staff : AuditableEntity, IMustHaveTenant
{
    public Staff() : base(28)
    {
    }

    public Staff(
        Guid tenantId,
        string firstName,
        string lastName,
        string phone,
        string email,
        bool isActive = true,
        Guid? userId = null) : base(28)
    {
        TenantId = tenantId;
        UserId = userId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        IsActive = isActive;
    }

    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<StaffBranch> Branches { get; set; } = [];
    public ICollection<StaffService> Services { get; set; } = [];
    public ICollection<StaffWeeklyScheduleDay> WeeklySchedule { get; set; } = [];

    public void Update(
        string firstName,
        string lastName,
        string phone,
        string email,
        bool isActive)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        IsActive = isActive;
    }

    public void SetActive(bool isActive) =>
        IsActive = isActive;
}
