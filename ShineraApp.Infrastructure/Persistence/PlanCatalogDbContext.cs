using Infrastructure.SharedKernel.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using ShineraApp.Application.Interfaces;
using ShineraApp.Domain.Entities;

namespace ShineraApp.Infrastructure.Persistence;

public sealed class PlanCatalogDbContext(DbContextOptions<PlanCatalogDbContext> options)
    : BaseDbContext(options), IPlanCatalogDbContext
{
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanCatalogDbContext).Assembly,
            type => type.Namespace == "ShineraApp.Infrastructure.Persistence.Configurations");
    }
}
