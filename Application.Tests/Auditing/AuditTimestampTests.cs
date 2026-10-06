using Domain.SharedKernel.Entities;

namespace Application.Tests.Auditing;

public sealed class AuditTimestampTests
{
    [Fact]
    public void Create_NormalizesTimestampToUtc()
    {
        var entity = new AuditProbe();
        var userId = Guid.NewGuid();
        var localInstant = new DateTimeOffset(
            2026, 10, 5, 21, 30, 0,
            TimeSpan.FromHours(3.5));

        entity.Create(userId, "127.0.0.1", localInstant);

        Assert.Equal(TimeSpan.Zero, entity.CreatedAt.Offset);
        Assert.Equal(localInstant.UtcDateTime, entity.CreatedAt.UtcDateTime);
        Assert.Equal(userId, entity.CreatedBy);
        Assert.Null(entity.LastModifiedAt);
    }

    [Fact]
    public void Modify_NormalizesTimestampToUtc()
    {
        var entity = new AuditProbe();
        var userId = Guid.NewGuid();
        var localInstant = new DateTimeOffset(
            2026, 10, 5, 22, 0, 0,
            TimeSpan.FromHours(4));

        entity.Modify(userId, "127.0.0.1", localInstant);

        Assert.Equal(TimeSpan.Zero, entity.LastModifiedAt!.Value.Offset);
        Assert.Equal(localInstant.UtcDateTime, entity.LastModifiedAt.Value.UtcDateTime);
        Assert.Equal(userId, entity.LastModifiedBy);
    }

    [Fact]
    public void Delete_NormalizesTimestampToUtc()
    {
        var entity = new FullAuditProbe();
        var userId = Guid.NewGuid();
        var localInstant = new DateTimeOffset(
            2026, 10, 6, 0, 15, 0,
            TimeSpan.FromHours(4));

        entity.Delete(userId, "127.0.0.1", localInstant);

        Assert.True(entity.IsDeleted);
        Assert.Equal(TimeSpan.Zero, entity.DeletedAt!.Value.Offset);
        Assert.Equal(localInstant.UtcDateTime, entity.DeletedAt.Value.UtcDateTime);
        Assert.Equal(userId, entity.DeletedBy);
    }

    private sealed class AuditProbe : AuditableEntity
    {
    }

    private sealed class FullAuditProbe : FullAuditableEntity
    {
    }
}
