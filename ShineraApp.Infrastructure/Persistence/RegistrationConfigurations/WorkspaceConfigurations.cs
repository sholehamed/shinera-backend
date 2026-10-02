using Domain.SharedKernel.Entities;
using Infrastructure.SharedKernel.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShineraApp.Domain.Entities;
namespace ShineraApp.Infrastructure.Persistence.RegistrationConfigurations;

public abstract class WorkspaceConfiguration<T> : FullAuditEntityConfiguration<T> where T : FullAuditableEntity
{
    public override void Configure(EntityTypeBuilder<T> b) { base.Configure(b); b.Ignore(x => x.DomainEvents); }
}
public sealed class OwnerConfiguration : WorkspaceConfiguration<OwnerAccount>
{
    public override void Configure(EntityTypeBuilder<OwnerAccount> b)
    {
        base.Configure(b); b.ToTable("Owners", "Workspace");
        b.Property(x => x.FirstName).HasMaxLength(60); b.Property(x => x.LastName).HasMaxLength(80);
        b.Property(x => x.Email).HasMaxLength(254); b.Property(x => x.Mobile).HasMaxLength(11);
        b.Property(x => x.PasswordHash).HasMaxLength(512);
        b.HasIndex(x => x.Email).IsUnique(); b.HasIndex(x => x.Mobile).IsUnique();
    }
}
public sealed class TenantConfiguration : WorkspaceConfiguration<Tenant>
{
    public override void Configure(EntityTypeBuilder<Tenant> b)
    { base.Configure(b); b.ToTable("Tenants", "Workspace"); b.Property(x => x.Name).HasMaxLength(120);
      b.Property(x => x.Slug).HasMaxLength(40); b.HasIndex(x => x.Slug).IsUnique(); }
}
public sealed class BranchConfiguration : WorkspaceConfiguration<Branch>
{
    public override void Configure(EntityTypeBuilder<Branch> b)
    { base.Configure(b); b.ToTable("Branches", "Workspace"); b.Property(x => x.Name).HasMaxLength(120);
      b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
      b.HasAlternateKey(x => new { x.Id, x.TenantId });
      b.HasIndex(x => x.TenantId).IsUnique().HasFilter("[IsMain] = 1 AND [IsDeleted] = 0"); }
}
public sealed class ProfileConfiguration : WorkspaceConfiguration<BusinessProfile>
{
    public override void Configure(EntityTypeBuilder<BusinessProfile> b)
    { base.Configure(b); b.ToTable("BusinessProfiles", "Workspace"); b.HasIndex(x => x.TenantId).IsUnique();
      b.HasOne<Tenant>().WithOne().HasForeignKey<BusinessProfile>(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
      b.Property(x => x.ActivityType).HasMaxLength(60); b.Property(x => x.Phone).HasMaxLength(20);
      b.Property(x => x.City).HasMaxLength(80); b.Property(x => x.Address).HasMaxLength(400);
      b.Property(x => x.PostalCode).HasMaxLength(20); b.Property(x => x.Instagram).HasMaxLength(80); }
}
public sealed class TenantMembershipConfiguration : WorkspaceConfiguration<TenantMembership>
{
    public override void Configure(EntityTypeBuilder<TenantMembership> b)
    { base.Configure(b); b.ToTable("TenantMemberships", "Workspace"); b.HasIndex(x => new { x.TenantId, x.OwnerId }).IsUnique();
      b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
      b.HasOne<OwnerAccount>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict); }
}
public sealed class BranchMembershipConfiguration : WorkspaceConfiguration<BranchMembership>
{
    public override void Configure(EntityTypeBuilder<BranchMembership> b)
    { base.Configure(b); b.ToTable("BranchMemberships", "Workspace"); b.HasIndex(x => new { x.BranchId, x.OwnerId }).IsUnique();
      b.HasOne<Branch>().WithMany().HasForeignKey(x => new { x.BranchId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
      b.HasOne<OwnerAccount>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict); }
}
public sealed class SubscriptionConfiguration : WorkspaceConfiguration<TenantSubscription>
{
    public override void Configure(EntityTypeBuilder<TenantSubscription> b)
    { base.Configure(b); b.ToTable("Subscriptions", "Workspace"); b.Property(x => x.Amount).HasPrecision(18, 2);
      b.Property(x => x.Currency).HasMaxLength(3); b.HasIndex(x => x.TenantId);
      b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
      b.HasOne<Plan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
      b.HasOne<PlanPrice>().WithMany().HasForeignKey(x => x.PriceId).OnDelete(DeleteBehavior.Restrict); }
}
public sealed class RegistrationConfiguration : WorkspaceConfiguration<WorkspaceRegistration>
{
    public override void Configure(EntityTypeBuilder<WorkspaceRegistration> b)
    { base.Configure(b); b.ToTable("Registrations", "Workspace"); b.HasIndex(x => x.RequestId).IsUnique();
      b.Property(x => x.Fingerprint).HasMaxLength(64);
      b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
      b.HasOne<OwnerAccount>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
      b.HasOne<Branch>().WithMany().HasForeignKey(x => new { x.BranchId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
      b.HasOne<Plan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
      b.HasOne<PlanPrice>().WithMany().HasForeignKey(x => x.PriceId).OnDelete(DeleteBehavior.Restrict); }
}
