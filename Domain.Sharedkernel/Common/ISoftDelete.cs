namespace Domain.SharedKernel.Common;

public interface ISoftDelete
{
    bool IsDeleted { get; }
    Guid? DeletedBy { get; }
    DateTimeOffset? DeletedAt { get; }
    string? DeletedByIp { get; }

    void Delete(Guid userId, string? ip, DateTimeOffset timestamp);
}
