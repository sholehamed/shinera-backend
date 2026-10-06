using Modules.System.Workforce.Domain.Entities;

namespace Modules.System.Workforce.Application.Abstractions;

public interface IWorkforceDbContext : IBaseDbContext
{
    DbSet<Staff> Staff { get; }
    DbSet<StaffBranch> StaffBranches { get; }
    DbSet<StaffService> StaffServices { get; }
}
