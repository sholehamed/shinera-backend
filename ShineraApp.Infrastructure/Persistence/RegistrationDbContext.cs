using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ShineraApp.Application.Features.Registration;
using ShineraApp.Domain.Entities;

namespace ShineraApp.Infrastructure.Persistence;

// Privileged bootstrap unit of work. Never inject into tenant data read endpoints.
// No client-provided TenantId/UserId is accepted by registration.
public sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options)
    : DbContext(options), IRegistrationDbContext
{
    public DbSet<OwnerAccount> Owners => Set<OwnerAccount>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<BranchMembership> BranchMemberships => Set<BranchMembership>();
    public DbSet<TenantSubscription> Subscriptions => Set<TenantSubscription>();
    public DbSet<WorkspaceRegistration> Registrations => Set<WorkspaceRegistration>();
    public DbSet<Plan> Plans => Set<Plan>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegistrationDbContext).Assembly);
        // Catalog is mapped for transactional authoritative pricing, owned by its own migrations.
        modelBuilder.Entity<Plan>().ToTable("Plans", "Catalog", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<PlanPrice>().ToTable("PlanPrices", "Catalog", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Feature>().ToTable("Features", "Catalog", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<PlanFeature>().ToTable("PlanFeatures", "Catalog", t => t.ExcludeFromMigrations());
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new RegistrationConflictException(); }
    }
}
