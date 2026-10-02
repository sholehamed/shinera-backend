using Application.SharedKernel.Abstractions.Messaging;
using Application.Sharedkernel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ShineraApp.Domain.Entities;

namespace ShineraApp.Application.Features.Registration;

public sealed class RegistrationOptions { public bool Enabled { get; set; } }
public sealed record RegisterWorkspaceCommand(Guid RequestId, string PlanKey, string BillingCycle,
    string FirstName, string LastName, string Email, string Mobile, string Password,
    string BusinessName, string Slug, string ActivityType, string Phone, string City,
    string Address, string PostalCode, string Instagram, bool AcceptTerms, bool AcceptPrivacy)
    : ICommand<Result<RegistrationReceipt>>
{
    public override string ToString() => nameof(RegisterWorkspaceCommand);
}
public sealed record RegistrationReceipt(Guid TenantId, Guid BranchId, string Slug);

public interface IRegistrationDbContext
{
    DbSet<OwnerAccount> Owners { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<Branch> Branches { get; }
    DbSet<BusinessProfile> BusinessProfiles { get; }
    DbSet<TenantMembership> TenantMemberships { get; }
    DbSet<BranchMembership> BranchMemberships { get; }
    DbSet<TenantSubscription> Subscriptions { get; }
    DbSet<WorkspaceRegistration> Registrations { get; }
    DbSet<Plan> Plans { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
public interface IOwnerPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public sealed class RegistrationConflictException : Exception { }
