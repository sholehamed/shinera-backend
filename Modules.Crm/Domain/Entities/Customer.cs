namespace Modules.System.Crm.Domain.Entities;

public sealed class Customer : AuditableEntity, IMustHaveTenant
{
    public Customer() : base(33)
    {
    }

    public Customer(
        Guid tenantId,
        string firstName,
        string lastName,
        string mobile,
        string normalizedMobile,
        string? email,
        DateOnly? birthday,
        string? gender,
        string? notes,
        bool isVip,
        Guid? userId = null,
        bool isActive = true) : base(33)
    {
        TenantId = tenantId;
        UserId = userId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Mobile = mobile.Trim();
        NormalizedMobile = normalizedMobile;
        Email = NormalizeOptional(email);
        Birthday = birthday;
        Gender = NormalizeOptional(gender);
        Notes = NormalizeOptional(notes);
        IsVip = isVip;
        IsActive = isActive;
    }

    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Mobile { get; set; } = default!;
    public string NormalizedMobile { get; set; } = default!;
    public string? Email { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? Gender { get; set; }
    public string? Notes { get; set; }
    public bool IsVip { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CustomerNote> NoteEntries { get; set; } = [];

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
