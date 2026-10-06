using Domain.SharedKernel.Common;
using Domain.SharedKernel.Common.Events;

namespace Domain.SharedKernel.Entities;

public interface IAuditableEntity : IAuditable, IEntity, IHasDomainEvents
{
}

public abstract class AuditableEntity : Entity, IAuditableEntity
{
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public Guid? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedByIp { get; set; }

    protected AuditableEntity() : base(Guid.CreateVersion7())
    {
    }

    protected AuditableEntity(ushort tableCode) : base(tableCode)
    {
    }

    protected AuditableEntity(ulong id, ushort tableCode) : base(id, tableCode)
    {
    }

    protected AuditableEntity(Guid id) : base(id)
    {
    }

    public void Create(Guid userId, string? ip, DateTimeOffset timestamp)
    {
        CreatedBy = userId;
        CreatedByIp = ip;
        CreatedAt = timestamp.ToUniversalTime();
    }

    public void Modify(Guid userId, string? ip, DateTimeOffset timestamp)
    {
        LastModifiedBy = userId;
        LastModifiedByIp = ip;
        LastModifiedAt = timestamp.ToUniversalTime();
    }
}
