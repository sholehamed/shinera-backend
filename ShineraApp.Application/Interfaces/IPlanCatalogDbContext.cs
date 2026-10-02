using Microsoft.EntityFrameworkCore;
using ShineraApp.Domain.Entities;

namespace ShineraApp.Application.Interfaces;

// Plans are platform-owned reference data; no client tenant selector is accepted.
public interface IPlanCatalogDbContext
{
    DbSet<Plan> Plans { get; }
}
