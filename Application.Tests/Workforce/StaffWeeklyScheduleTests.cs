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

public sealed class StaffWeeklyScheduleTests
{
    [Fact]
    public async Task Validator_RejectsIncompleteWeek()
    {
        var validator =
            new ReplaceStaffWeeklyScheduleCommandValidator();

        var result = await validator.ValidateAsync(
            new ReplaceStaffWeeklyScheduleCommand(
                Guid.NewGuid(),
                [
                    WorkingDay(
                        DayOfWeek.Saturday,
                        new TimeOnly(9, 0),
                        new TimeOnly(18, 0))
                ]));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            x => x.ErrorMessage.Contains(
                "exactly seven days",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validator_RejectsDayOffWithWorkingHours()
    {
        var validator =
            new ReplaceStaffWeeklyScheduleCommandValidator();

        var days = BuildCompleteWeek();
        days[6] = new StaffScheduleDayInput(
            DayOfWeek.Friday,
            true,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            []);

        var result = await validator.ValidateAsync(
            new ReplaceStaffWeeklyScheduleCommand(
                Guid.NewGuid(),
                days));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            x => x.ErrorMessage.Contains(
                "day off",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validator_RejectsBreakOutsideHoursAndOverlap()
    {
        var validator =
            new ReplaceStaffWeeklyScheduleCommandValidator();

        var days = BuildCompleteWeek();
        days[0] = WorkingDay(
            DayOfWeek.Saturday,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0),
            [
                new StaffScheduleBreakInput(
                    new TimeOnly(8, 30),
                    new TimeOnly(9, 30)),
                new StaffScheduleBreakInput(
                    new TimeOnly(13, 0),
                    new TimeOnly(14, 0)),
                new StaffScheduleBreakInput(
                    new TimeOnly(13, 30),
                    new TimeOnly(15, 0))
            ]);

        var result = await validator.ValidateAsync(
            new ReplaceStaffWeeklyScheduleCommand(
                Guid.NewGuid(),
                days));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            x => x.ErrorMessage.Contains(
                "inside working hours",
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Errors,
            x => x.ErrorMessage.Contains(
                "overlap",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReplaceAndQuery_PersistsCompleteWeekWithBreaks()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();

        var replace = new ReplaceStaffWeeklyScheduleCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        await replace.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                staff.Id,
                BuildCompleteWeek()),
            CancellationToken.None);

        var query = new StaffWeeklyScheduleQueryHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var schedule = await query.Handle(
            new StaffWeeklyScheduleQuery(staff.Id),
            CancellationToken.None);

        Assert.Equal(7, schedule.Days.Count);
        Assert.Equal(
            DayOfWeek.Saturday,
            schedule.Days[0].DayOfWeek);
        Assert.Equal(
            DayOfWeek.Friday,
            schedule.Days[6].DayOfWeek);

        var saturday = schedule.Days[0];
        Assert.False(saturday.IsDayOff);
        Assert.Equal(
            new TimeOnly(9, 0),
            saturday.StartTime);
        Assert.Equal(
            new TimeOnly(18, 0),
            saturday.EndTime);

        var scheduleBreak =
            Assert.Single(saturday.Breaks);

        Assert.Equal(
            new TimeOnly(13, 0),
            scheduleBreak.StartTime);
        Assert.Equal(
            new TimeOnly(14, 0),
            scheduleBreak.EndTime);

        Assert.True(schedule.Days[6].IsDayOff);
        Assert.Null(schedule.Days[6].StartTime);
        Assert.Null(schedule.Days[6].EndTime);
    }

    [Fact]
    public async Task ReplaceSchedule_UpdatesBreaksWithoutDuplicates()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();
        var handler = new ReplaceStaffWeeklyScheduleCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        await handler.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                staff.Id,
                BuildCompleteWeek()),
            CancellationToken.None);

        var updated = BuildCompleteWeek();
        updated[0] = WorkingDay(
            DayOfWeek.Saturday,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0),
            [
                new StaffScheduleBreakInput(
                    new TimeOnly(12, 30),
                    new TimeOnly(13, 15))
            ]);

        await handler.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                staff.Id,
                updated),
            CancellationToken.None);

        var saturday = await fixture.Db.StaffWeeklyScheduleDays
            .AsNoTracking()
            .Include(x => x.Breaks)
            .SingleAsync(
                x =>
                    x.StaffId == staff.Id &&
                    x.DayOfWeek == DayOfWeek.Saturday);

        var scheduleBreak =
            Assert.Single(saturday.Breaks);

        Assert.Equal(
            new TimeOnly(12, 30),
            scheduleBreak.StartTime);
        Assert.Equal(
            new TimeOnly(13, 15),
            scheduleBreak.EndTime);
    }

    [Fact]
    public async Task OwnScope_AllowsLinkedStaffAndRejectsDifferentStaff()
    {
        await using var fixture = await CreateFixtureAsync();

        var ownStaff = await fixture.AddStaffAsync(
            fixture.CurrentUserId);

        var otherStaff = await fixture.AddStaffAsync(
            Guid.NewGuid());

        var ownAuthorization =
            new ScopedAuthorizationService(
                PermissionScopeType.Own,
                fixture.CurrentUserId);

        var handler =
            new ReplaceStaffWeeklyScheduleCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                ownAuthorization);

        await handler.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                ownStaff.Id,
                BuildCompleteWeek()),
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                new ReplaceStaffWeeklyScheduleCommand(
                    otherStaff.Id,
                    BuildCompleteWeek()),
                CancellationToken.None));
    }

    [Fact]
    public async Task TenantFilter_HidesForeignStaffSchedule()
    {
        await using var fixture = await CreateFixtureAsync();

        var foreignTenantId = Guid.NewGuid();
        var foreignStaff = new StaffEntity(
            foreignTenantId,
            "Foreign",
            "Staff",
            "1",
            "foreign@example.test");

        var foreignDay =
            new StaffWeeklyScheduleDay(
                foreignTenantId,
                foreignStaff.Id,
                DayOfWeek.Saturday,
                false,
                new TimeOnly(9, 0),
                new TimeOnly(18, 0));

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.Staff.Add(foreignStaff);
            fixture.Db.StaffWeeklyScheduleDays.Add(
                foreignDay);

            await fixture.Db.SaveChangesAsync();
        }

        var count = await fixture.Db.StaffWeeklyScheduleDays
            .AsNoTracking()
            .CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Availability_ExcludesBreaksFromWorkingSegments()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();

        var replace = new ReplaceStaffWeeklyScheduleCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        await replace.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                staff.Id,
                BuildCompleteWeek()),
            CancellationToken.None);

        var service =
            new StaffScheduleAvailabilityService(
                fixture.Db);

        // 2026-10-10 is Saturday.
        var availability = await service.ResolveAsync(
            staff.Id,
            new DateOnly(2026, 10, 10));

        Assert.True(availability.IsConfigured);
        Assert.True(availability.IsStaffActive);
        Assert.False(availability.IsDayOff);
        Assert.Equal(2, availability.AvailableSegments.Count);
        Assert.Equal(
            new ScheduleTimeSegment(
                new TimeOnly(9, 0),
                new TimeOnly(13, 0)),
            availability.AvailableSegments[0]);
        Assert.Equal(
            new ScheduleTimeSegment(
                new TimeOnly(14, 0),
                new TimeOnly(18, 0)),
            availability.AvailableSegments[1]);
    }

    [Fact]
    public async Task Availability_UnconfiguredStaffIsExplicitlyUnconfigured()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();

        var service =
            new StaffScheduleAvailabilityService(
                fixture.Db);

        var availability = await service.ResolveAsync(
            staff.Id,
            new DateOnly(2026, 10, 10));

        Assert.False(availability.IsConfigured);
        Assert.Empty(availability.AvailableSegments);
    }

    [Fact]
    public async Task Availability_DayOffHasNoSegments()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();
        var replace = new ReplaceStaffWeeklyScheduleCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        await replace.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                staff.Id,
                BuildCompleteWeek()),
            CancellationToken.None);

        var service =
            new StaffScheduleAvailabilityService(
                fixture.Db);

        // 2026-10-09 is Friday.
        var availability = await service.ResolveAsync(
            staff.Id,
            new DateOnly(2026, 10, 9));

        Assert.True(availability.IsConfigured);
        Assert.True(availability.IsDayOff);
        Assert.Empty(availability.AvailableSegments);
    }

    [Fact]
    public async Task Availability_InactiveStaffHasNoSegments()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync(
            isActive: false);

        var replace = new ReplaceStaffWeeklyScheduleCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        await replace.Handle(
            new ReplaceStaffWeeklyScheduleCommand(
                staff.Id,
                BuildCompleteWeek()),
            CancellationToken.None);

        var service =
            new StaffScheduleAvailabilityService(
                fixture.Db);

        var availability = await service.ResolveAsync(
            staff.Id,
            new DateOnly(2026, 10, 10));

        Assert.True(availability.IsConfigured);
        Assert.False(availability.IsStaffActive);
        Assert.Empty(availability.AvailableSegments);
    }

    [Fact]
    public async Task BranchScope_CannotManageWeeklySchedule()
    {
        await using var fixture = await CreateFixtureAsync();

        var staff = await fixture.AddStaffAsync();

        var authorization =
            new ScopedAuthorizationService(
                PermissionScopeType.Branch,
                fixture.CurrentUserId);

        var handler =
            new ReplaceStaffWeeklyScheduleCommandHandler(
                fixture.Db,
                fixture.TenantContext,
                authorization);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                new ReplaceStaffWeeklyScheduleCommand(
                    staff.Id,
                    BuildCompleteWeek()),
                CancellationToken.None));
    }

    private static List<StaffScheduleDayInput> BuildCompleteWeek() =>
    [
        WorkingDay(
            DayOfWeek.Saturday,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0),
            [
                new StaffScheduleBreakInput(
                    new TimeOnly(13, 0),
                    new TimeOnly(14, 0))
            ]),
        WorkingDay(
            DayOfWeek.Sunday,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0)),
        WorkingDay(
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0)),
        WorkingDay(
            DayOfWeek.Tuesday,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0)),
        WorkingDay(
            DayOfWeek.Wednesday,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0)),
        WorkingDay(
            DayOfWeek.Thursday,
            new TimeOnly(9, 0),
            new TimeOnly(18, 0)),
        new StaffScheduleDayInput(
            DayOfWeek.Friday,
            true,
            null,
            null,
            [])
    ];

    private static StaffScheduleDayInput WorkingDay(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        IReadOnlyCollection<StaffScheduleBreakInput>? breaks = null) =>
        new(
            dayOfWeek,
            false,
            startTime,
            endTime,
            breaks ?? []);

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var tenantContext = new TenantContext();
        tenantContext.Initialize(
            userId,
            false,
            [tenantId],
            [tenantId],
            tenantId);

        var connection =
            new SqliteConnection("Data Source=:memory:");

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
            tenantId,
            userId);
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

            foreach (var entityType in builder.Model.GetEntityTypes())
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

    private sealed class ScopedAuthorizationService(
        PermissionScopeType scope,
        Guid currentUserId)
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
            CancellationToken cancellationToken = default)
        {
            var allowed =
                scope == PermissionScopeType.Tenant ||
                scope == PermissionScopeType.Own &&
                resourceContext.OwnerUserId.HasValue &&
                resourceContext.OwnerUserId == currentUserId &&
                userId == currentUserId;

            return Task.FromResult(
                allowed
                    ? PermissionDecision.Allow(
                        $"{resource}.{action}",
                        scope)
                    : PermissionDecision.Deny(
                        $"{resource}.{action}",
                        PermissionDecisionCode.ScopeDenied));
        }

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
        Guid tenantId,
        Guid currentUserId)
        : IAsyncDisposable
    {
        public WorkforceDbContext Db { get; } = db;
        public TenantContext TenantContext { get; } =
            tenantContext;
        public Guid TenantId { get; } = tenantId;
        public Guid CurrentUserId { get; } =
            currentUserId;

        public IPermissionAuthorizationService
            TenantAuthorization { get; } =
            new ScopedAuthorizationService(
                PermissionScopeType.Tenant,
                currentUserId);

        public async Task<StaffEntity> AddStaffAsync(
            Guid? linkedUserId = null,
            bool isActive = true)
        {
            var staff = new StaffEntity(
                TenantId,
                "Sara",
                "Staff",
                Guid.NewGuid().ToString("N")[..11],
                $"{Guid.NewGuid():N}@example.test",
                isActive,
                linkedUserId);

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
