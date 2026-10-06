using Modules.System.Crm.Domain.Entities;

namespace Modules.System.Crm.Application.Abstractions;

public interface ICrmDbContext : IBaseDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<CustomerNote> CustomerNotes { get; }
}
