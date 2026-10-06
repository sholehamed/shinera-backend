using Application.SharedKernel.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Workforce.Application.Features.Schedules;
using Modules.System.Workforce.Application.Scheduling;
using Modules.System.Workforce.Domain.Entities;
using Modules.System.Workforce.Infrastructure.Persistence.Contexts;
using Modules.System.Workforce.Infrastructure.Persistence.Interceptors;
using StaffEntity = Modules.System.Workforce.Domain.Entities.Staff;

namespace Application.Tests.Workforce;

public sealed class StaffScheduleTests
{
    [Fact]
    public async Task Validator_RequiresExactlySevenDistinctDays()
    {
        var validator =
            new ReplaceStaffWeeklyScheduleCommandValidator();

        var result = await validator.ValidateAsync(
            new ReplaceStaffWeeklyScheduleCommand(
                Guid.NewGuid(),
                [
                    WorkingDay(DayOfWeek.Saturday),
                    WorkingDay(DayOfWeek.Sunday)
                ]));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            x => x.ErrorMessage.Contains(
                "exactly seven days",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validator_RejectsInvalidBreaksAndDayOffPayload()
    {
        var validator =
            new ReplaceStaffWeeklyScheduleCommandValidator();

        var days = CompleteWeek();
        days[0] = new StaffScheduleDayInput(
            DayOfWeek.Saturday,
            false,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0),
            [
                new StaffScheduleBreakInput(
                    new TimeOnly(12, 0),
                    new TimeOnly(14, 0)),
                new StaffScheduleBreakInput(
                    new TimeOnly(13, 30),
                    new TimeOnly(15, 0))
            ]);

        days[1] = new StaffScheduleDayInput(
            DayOfWeek.Sunday,
            true,
            new TimeOnly(9, 0),
            null,
            []);

        var result = await validator.ValidateAsync(
            new ReplaceStaffWeeklyScheduleCommand(
                Guid.NewGuid(),
                days));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            x => x.ErrorMessage.Contains(
                "overlap",
                StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            result.Errors,
            x => x.ErrorMessage.Contains(
                "day off",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReplaceSchedule_PersistsCompleteWeekAndBreaks()
    {
        await using var fixture = await CreateFixtureAsync();
        var staff = await fixture.AddStaffAsync();

        var days = CompleteWeek();
        days[0] = new StaffScheduleDayInput(
            DayOfWeek.Saturday,
            false,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0),
            [
                new StaffScheduleBreakInput(
                    new TimeOnly(13, 0),
                    new TimeOnly(14, 0))
            ]);

        days[6] = DayOff(DayOfWeek.Friday);

        var handler =
            new ReplaceStaffWeeklyScheduleCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                fixture.TenantAuthorization);

        await handler.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                staff.Id,
                days),
            CancellationToken.None);

        var storedDays = await fixture.Db.StaffWeeklyScheduleDays
            .AsNoTracking()
            .Where(x => x.StaffId == staff.Id)
            .Include(x => x.Breaks)
            .ToListAsync();

        Assert.Equal(7, storedDays.Count);

        var saturday = storedDays.Single(
            x => x.DayOfWeek == DayOfWeek.Saturday);

        Assert.Equal(
            new TimeOnly(9, 0),
            saturday.StartTime);

        Assert.Single(saturday.Breaks);

        var friday = storedDays.Single(
            x => x.DayOfWeek == DayOfWeek.Friday);

        Assert.True(friday.IsDayOff);
        Assert.Null(friday.StartTime);
        Assert.Null(friday.EndTime);
        Assert.Empty(friday.Breaks);
    }

    [Fact]
    public async Task ScheduleQuery_ReturnsSaturdayFirst()
    {
        await using var fixture = await CreateFixtureAsync();
        var staff = await fixture.AddStaffAsync();

        var handler =
            new ReplaceStaffWeeklyScheduleCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                fixture.TenantAuthorization);

        await handler.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                staff.Id,
                CompleteWeek()),
            CancellationToken.None);

        var query =
            new StaffWeeklyScheduleQueryHandler(
                fixture.Db,
                fixture.TenantContext,
                fixture.TenantAuthorization);

        var result = await query.Handle(
            new StaffWeeklyScheduleQuery(staff.Id),
            CancellationToken.None);

        Assert.Equal(7, result.Days.Count);
        Assert.Equal(
            DayOfWeek.Saturday,
            result.Days[0].DayOfWeek);
        Assert.Equal(
            DayOfWeek.Friday,
            result.Days[^1].DayOfWeek);
    }

    [Fact]
    public async Task AvailabilityResolver_SubtractsBreaks()
    {
        await using var fixture = await CreateFixtureAsync();
        var staff = await fixture.AddStaffAsync();

        var saturday = new StaffWeeklyScheduleDay(
            fixture.TenantId,
            staff.Id,
            DayOfWeek.Saturday,
            false,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0));

        saturday.Breaks.Add(
            new StaffScheduleBreak(
                fixture.TenantId,
                saturday.Id,
                new TimeOnly(13, 0),
                new TimeOnly(14, 0)));

        fixture.Db.StaffWeeklyScheduleDays.Add(saturday);
        await fixture.Db.SaveChangesAsync();

        var service =
            new StaffScheduleAvailabilityService(
                fixture.Db);

        var date = FindNext(
            DayOfWeek.Saturday);

        var result = await service.ResolveAsync(
            staff.Id,
            date);

        Assert.True(result.IsConfigured);
        Assert.True(result.IsStaffActive);
        Assert.False(result.IsDayOff);
        Assert.Equal(2, result.AvailableSegments.Count);

        Assert.Equal(
            new ScheduleTimeSegment(
                new TimeOnly(9, 0),
                new TimeOnly(13, 0)),
            result.AvailableSegments[0]);

        Assert.Equal(
            new ScheduleTimeSegment(
                new TimeOnly(14, 0),
                new TimeOnly(18, 0)),
            result.AvailableSegments[1]);
    }

    [Fact]
    public async Task AvailabilityResolver_DayOffHasNoSegments()
    {
        await using var fixture = await CreateFixtureAsync();
        var staff = await fixture.AddStaffAsync();

        fixture.Db.StaffWeeklyScheduleDays.Add(
            new StaffWeeklyScheduleDay(
                fixture.TenantId,
                staff.Id,
                DayOfWeek.Friday,
                true,
                null,
                null));

        await fixture.Db.SaveChangesAsync();

        var service =
            new StaffScheduleAvailabilityService(
                fixture.Db);

        var result = await service.ResolveAsync(
            staff.Id,
            FindNext(DayOfWeek.Friday));

        Assert.True(result.IsConfigured);
        Assert.True(result.IsDayOff);
        Assert.Empty(result.AvailableSegments);
    }

    [Fact]
    public async Task ReplaceSchedule_WithBranchScope_IsDenied()
    {
        await using var fixture = await CreateFixtureAsync();
        var staff = await fixture.AddStaffAsync();

        var handler =
            new ReplaceStaffWeeklyScheduleCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                new StubAuthorizationService(
                    PermissionScopeType.Branch));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                new ReplaceStaffWeeklyScheduleCommand(
                    staff.Id,
                    CompleteWeek()),
                CancellationToken.None));
    }

    [Fact]
    public async Task TenantFilter_HidesAnotherTenantsSchedule()
    {
        await using var fixture = await CreateFixtureAsync();

        var foreignTenantId = Guid.NewGuid();
        var foreignStaff = new StaffEntity(
            foreignTenantId,
            "Foreign",
            "Staff",
            "1",
            "foreign@example.test");

        var foreignDay = new StaffWeeklyScheduleDay(
            foreignTenantId,
            foreignStaff.Id,
            DayOfWeek.Saturday,
            false,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0));

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.Staff.Add(foreignStaff);
            fixture.Db.StaffWeeklyScheduleDays.Add(foreignDay);
            await fixture.Db.SaveChangesAsync();
        }

        var count = await fixture.Db.StaffWeeklyScheduleDays
            .AsNoTracking()
            .CountAsync();

        Assert.Equal(0, count);
    }

    private static List<StaffScheduleDayInput> CompleteWeek() =>
    [
        WorkingDay(DayOfWeek.Saturday),
        WorkingDay(DayOfWeek.Sunday),
        WorkingDay(DayOfWeek.Monday),
        WorkingDay(DayOfWeek.Tuesday),
        WorkingDay(DayOfWeek.Wednesday),
        WorkingDay(DayOfWeek.Thursday),
        WorkingDay(DayOfWeek.Friday)
    ];

    private static StaffScheduleDayInput WorkingDay(
        DayOfWeek day) =>
        new(
            day,
            false,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0),
            []);

    private static StaffScheduleDayInput DayOff(
        DayOfWeek day) =>
        new(
            day,
            true,
            null,
            null,
            []);

    private static DateOnly FindNext(
        DayOfWeek target)
    {
        var date = new DateOnly(2026, 1, 1);

        while (date.DayOfWeek != target)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var tenantId = Guid.NewGuid();

        var tenantContext = new TenantContext();
        tenantContext.Initialize(
            Guid.NewGuid(),
            false,
            [tenantId],
            [tenantId],
            tenantId);

        var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<WorkforceDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(
                    new WorkforceTenantSaveChangesInterceptor(
                        tenantContext))
                .Options;

        var db = new TestWorkforceDbContext(
            options,
            tenantContext);

        await db.Database.EnsureCreatedAsync();

        return new Fixture(
            connection,
            db,
            tenantContext,
            tenantId);
    }

    private sealed class TestWorkforceDbContext(
        DbContextOptions<WorkforceDbContext> options,
        TenantContext tenantContext)
        : WorkforceDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in
                     builder.Model.GetEntityTypes())
            {
                var rowVersion =
                    entityType.FindProperty("RowVersion");

                if (rowVersion is not null)
                {
                    rowVersion.ValueGenerated =
                        ValueGenerated.Never;
                }
            }
        }
    }

    private sealed class StubAuthorizationService(
        PermissionScopeType scope)
        : IPermissionAuthorizationService
    {
        public Task<PermissionDecision> HasPermissionAsync(
            Guid userId,
            string resource,
            string action,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                PermissionDecision.Allow(
                    $"{resource}.{action}",
                    scope));

        public Task<PermissionDecision> AuthorizeAsync(
            Guid userId,
            string resource,
            string action,
            PermissionScopeContext resourceContext,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                PermissionDecision.Allow(
                    $"{resource}.{action}",
                    scope));

        public Task<IReadOnlyList<EffectivePermissionDto>>
            GetGrantedScopesAsync(
                Guid userId,
                string resource,
                string action,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EffectivePermissionDto>>(
            [
                new EffectivePermissionDto(
                    $"{resource}.{action}",
                    scope.ToString(),
                    null)
            ]);

        public Task<IReadOnlyList<EffectivePermissionDto>>
            GetEffectivePermissionsAsync(
                Guid userId,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class Fixture(
        SqliteConnection connection,
        WorkforceDbContext db,
        TenantContext tenantContext,
        Guid tenantId)
        : IAsyncDisposable
    {
        public WorkforceDbContext Db { get; } = db;
        public TenantContext TenantContext { get; } =
            tenantContext;
        public Guid TenantId { get; } = tenantId;

        public IPermissionAuthorizationService
            TenantAuthorization { get; } =
            new StubAuthorizationService(
                PermissionScopeType.Tenant);

        public async Task<StaffEntity> AddStaffAsync(
            bool isActive = true)
        {
            var staff = new StaffEntity(
                TenantId,
                "Sara",
                "Staff",
                "09120000000",
                $"{Guid.NewGuid():N}@example.test",
                isActive);

            Db.Staff.Add(staff);
            await Db.SaveChangesAsync();
            return staff;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
