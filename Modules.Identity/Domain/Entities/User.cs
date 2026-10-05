using Domain.SharedKernel.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Modules.System.Identity.Domain.Entities;

public class User : AuditableEntity
{
    public User() : base(17)
    {
    }

    public User(ulong id) : base(id, 17)
    {
    }

    public User(Guid id) : base(id)
    {
    }

    [Obsolete("M0.1 compatibility constructor for legacy development seed data only.")]
    public User(
        Guid legacyTenantId,
        string userName,
        string email,
        string? firstName,
        string? lastName,
        bool isActive = true) : base(17)
    {
        TenantId = legacyTenantId;
        SetIdentity(userName, email, firstName, lastName, isActive);
    }

    [Obsolete("M0.1 compatibility constructor for legacy development seed data only.")]
    public User(
        Guid id,
        Guid legacyTenantId,
        string userName,
        string email,
        string? firstName,
        string? lastName,
        bool isActive = true) : base(id)
    {
        TenantId = legacyTenantId;
        SetIdentity(userName, email, firstName, lastName, isActive);
    }

    public User(
        string userName,
        string email,
        string? firstName,
        string? lastName,
        bool isActive = true) : base(17)
    {
        SetIdentity(userName, email, firstName, lastName, isActive);
    }

    public User(
        Guid id,
        string userName,
        string email,
        string? firstName,
        string? lastName,
        bool isActive = true) : base(id)
    {
        SetIdentity(userName, email, firstName, lastName, isActive);
    }

    public Guid? ImageId { get; set; }

    // Transitional compile-only bridge for legacy seed code.
    // It is deliberately not persisted and must not be used for workspace authorization.
    [NotMapped]
    [Obsolete("User is global. Use TenantMembership.")]
    public Guid TenantId { get; set; }

    public string UserName { get; set; } = default!;
    public string NormalizedUserName { get; set; } = default!;

    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public bool EmailConfirmed { get; set; }

    public string PasswordHash { get; set; } = default!;
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public int AccessFailedCount { get; set; }

    public ICollection<TenantMembership> TenantMemberships { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserGroup> UserGroups { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];

    private void SetIdentity(
        string userName,
        string email,
        string? firstName,
        string? lastName,
        bool isActive)
    {
        UserName = userName;
        NormalizedUserName = userName.Trim().ToUpperInvariant();
        Email = email;
        NormalizedEmail = email.Trim().ToUpperInvariant();
        FirstName = firstName;
        LastName = lastName;
        IsActive = isActive;
    }
}
