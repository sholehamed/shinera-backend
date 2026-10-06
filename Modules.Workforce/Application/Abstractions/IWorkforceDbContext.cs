using Microsoft.EntityFrameworkCore.Infrastructure;
using Modules.System.Workforce.Domain.Entities;

namespace Modules.System.Workforce.Application.Abstractions;

public interface IWorkforceDbContext : IBaseDbContext
{
    DatabaseFacade Database { get; }
    DbSet<Staff> Staff { get; }
    DbSet<StaffBranch> StaffBranches { get; }
    DbSet<StaffService> StaffServices { get; }
    DbSet<StaffWeeklyScheduleDay> StaffWeeklyScheduleDays { get; }
    DbSet<StaffScheduleBreak> StaffScheduleBreaks { get; }
}
