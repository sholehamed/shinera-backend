namespace Domain.SharedKernel.Common;

public interface IAuditable
{
    Guid CreatedBy { get; }
    DateTimeOffset CreatedAt { get; }
    string? CreatedByIp { get; }
    Guid? LastModifiedBy { get; }
    DateTimeOffset? LastModifiedAt { get; }
    string? LastModifiedByIp { get; }

    void Create(Guid userId, string? ip, DateTimeOffset timestamp);
    void Modify(Guid userId, string? ip, DateTimeOffset timestamp);
}
