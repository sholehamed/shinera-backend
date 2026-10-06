using Modules.System.Services.Domain.Entities;
using ServiceEntity = Modules.System.Services.Domain.Entities.Service;

namespace Modules.System.Services.Application.Abstractions;

public interface IServiceCatalogDbContext : IBaseDbContext
{
    DbSet<ServiceCategory> ServiceCategories { get; }
    DbSet<ServiceEntity> Services { get; }
}
