using Domain.SharedKernel.Entities;
namespace ShineraApp.Domain.Entities;

public sealed class OwnerAccount : FullAuditableEntity
{
    private OwnerAccount() { }
    public OwnerAccount(string firstName, string lastName, string email, string mobile, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        FirstName = firstName.Trim(); LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant(); Mobile = mobile;
        PasswordHash = passwordHash;
    }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Mobile { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public bool EmailVerified { get; private set; }
    public bool MobileVerified { get; private set; }
}
