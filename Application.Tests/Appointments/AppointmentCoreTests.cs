using Application.SharedKernel.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Appointments.Application.Availability;
using Modules.System.Appointments.Application.Features.Appointments;
using Modules.System.Appointments.Domain.Entities;
using Modules.System.Appointments.Infrastructure.Persistence.Contexts;
using Modules.System.Crm.Domain.Entities;
using Modules.System.Crm.Infrastructure.Persistence.Contexts;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;
using Modules.System.Services.Domain.Entities;
using Modules.System.Services.Infrastructure.Persistence.Contexts;
using Modules.System.Workforce.Application.Scheduling;
using Modules.System.Workforce.Domain.Entities;
using Modules.System.Workforce.Infrastructure.Persistence.Contexts;
using ServiceEntity = Modules.System.Services.Domain.Entities.Service;
using StaffEntity = Modules.System.Workforce.Domain.Entities.Staff;

namespace Application.Tests.Appointments;

public sealed class AppointmentCoreTests
{
    [Fact]
    public void StatusMachine_AllowsHappyPathAndRejectsTerminalRegression()
    {
        var appointment = NewAppointment(
            AppointmentStatus.Pending);

        appointment.TransitionTo(
            AppointmentStatus.Confirmed);

        appointment.TransitionTo(
            AppointmentStatus.InProgress);

        appointment.TransitionTo(
            AppointmentStatus.Completed);

        Assert.Equal(
            AppointmentStatus.Completed,
            appointment.Status);

        Assert.False(
            appointment.CanTransitionTo(
                AppointmentStatus.InProgress));

        Assert.Throws<InvalidOperationException>(
            () => appointment.TransitionTo(
                AppointmentStatus.InProgress));
    }

    [Fact]
    public async Task AvailableSlots_RespectScheduleBreaksAndServiceDuration()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                durationMinutes: 30,
                includeBreak: true);

        var result = await fixture.Availability
            .GetAvailableSlotsAsync(
                fixture.BranchId,
                fixture.ServiceId,
                fixture.StaffId,
                fixture.Date);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            new[]
            {
                new TimeOnly(9, 0),
                new TimeOnly(9, 30),
                new TimeOnly(10, 30),
                new TimeOnly(11, 0),
                new TimeOnly(11, 30)
            },
            result.Value);
    }

    [Fact]
    public async Task CreateAppointment_DerivesEndTimeAndPriceFromService()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                durationMinutes: 60,
                price: 850_000m);

        var result = await fixture.CreateAsync(
            fixture.StaffId,
            new TimeOnly(9, 0));

        Assert.True(result.IsSuccess);

        var appointment =
            await fixture.Appointments.Appointments
                .AsNoTracking()
                .SingleAsync(x => x.Id == result.Value);

        Assert.Equal(
            new TimeOnly(10, 0),
            appointment.EndTime);

        Assert.Equal(
            850_000m,
            appointment.Price);

        Assert.Equal(
            AppointmentStatus.Confirmed,
            appointment.Status);
    }

    [Fact]
    public async Task SameStaffOverlappingAppointment_IsRejected()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var first = await fixture.CreateAsync(
            fixture.StaffId,
            new TimeOnly(9, 0));

        var overlap = await fixture.CreateAsync(
            fixture.StaffId,
            new TimeOnly(9, 30));

        Assert.True(first.IsSuccess);
        Assert.True(overlap.IsFailure);
        Assert.Equal(
            "appointment.conflict",
            overlap.Error.Code);
        Assert.Equal(
            ErrorType.Conflict,
            overlap.Error.Type);
    }

    [Fact]
    public async Task DifferentStaffSameTime_IsAllowed()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var otherStaffId =
            await fixture.AddStaffAsync();

        var first = await fixture.CreateAsync(
            fixture.StaffId,
            new TimeOnly(9, 0));

        var second = await fixture.CreateAsync(
            otherStaffId,
            new TimeOnly(9, 0));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
    }

    [Fact]
    public async Task AdjacentAppointment_IsAllowed()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var first = await fixture.CreateAsync(
            fixture.StaffId,
            new TimeOnly(9, 0));

        var adjacent = await fixture.CreateAsync(
            fixture.StaffId,
            new TimeOnly(10, 0));

        Assert.True(first.IsSuccess);
        Assert.True(adjacent.IsSuccess);
    }

    [Fact]
    public async Task CancelledAppointment_DoesNotBlockSlot()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        fixture.Appointments.Appointments.Add(
            new Appointment(
                fixture.TenantId,
                fixture.BranchId,
                fixture.CustomerId,
                fixture.StaffId,
                fixture.ServiceId,
                fixture.Date,
                new TimeOnly(9, 0),
                new TimeOnly(10, 0),
                500_000m,
                status: AppointmentStatus.Cancelled));

        await fixture.Appointments.SaveChangesAsync();

        var result = await fixture.CreateAsync(
            fixture.StaffId,
            new TimeOnly(9, 0));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task StaffMustBelongToBranchAndOfferService()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        var unassigned = new StaffEntity(
            fixture.TenantId,
            "Unassigned",
            "Staff",
            "09120000009",
            "unassigned@example.test");

        fixture.Workforce.Staff.Add(unassigned);
        await fixture.Workforce.SaveChangesAsync();

        var result = await fixture.CreateAsync(
            unassigned.Id,
            new TimeOnly(9, 0));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "appointment.staff_unavailable",
            result.Error.Code);
    }

    [Fact]
    public async Task AppointmentTenantFilter_HidesForeignAppointments()
    {
        await using var fixture =
            await Fixture.CreateAsync();

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Appointments.Appointments.Add(
                new Appointment(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    fixture.Date,
                    new TimeOnly(9, 0),
                    new TimeOnly(10, 0),
                    1));

            await fixture.Appointments.SaveChangesAsync();
        }

        Assert.Equal(
            0,
            await fixture.Appointments.Appointments.CountAsync());
    }

    private static Appointment NewAppointment(
        AppointmentStatus status) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 10),
            new TimeOnly(9, 0),
            new TimeOnly(10, 0),
            100,
            status: status);

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _identityConnection;
        private readonly SqliteConnection _servicesConnection;
        private readonly SqliteConnection _workforceConnection;
        private readonly SqliteConnection _crmConnection;
        private readonly SqliteConnection _appointmentsConnection;

        private Fixture(
            TenantContext tenantContext,
            SqliteConnection identityConnection,
            TestIdentityDbContext identity,
            SqliteConnection servicesConnection,
            TestServicesDbContext services,
            SqliteConnection workforceConnection,
            TestWorkforceDbContext workforce,
            SqliteConnection crmConnection,
            TestCrmDbContext crm,
            SqliteConnection appointmentsConnection,
            TestAppointmentsDbContext appointments,
            Guid tenantId,
            Guid branchId,
            Guid serviceId,
            Guid customerId,
            Guid staffId)
        {
            TenantContext = tenantContext;
            _identityConnection = identityConnection;
            Identity = identity;
            _servicesConnection = servicesConnection;
            Services = services;
            _workforceConnection = workforceConnection;
            Workforce = workforce;
            _crmConnection = crmConnection;
            Crm = crm;
            _appointmentsConnection = appointmentsConnection;
            Appointments = appointments;

            TenantId = tenantId;
            BranchId = branchId;
            ServiceId = serviceId;
            CustomerId = customerId;
            StaffId = staffId;

            Schedule =
                new StaffScheduleAvailabilityService(
                    Workforce);

            Availability =
                new AppointmentAvailabilityService(
                    Appointments,
                    Identity,
                    Services,
                    Workforce,
                    Schedule);

            Authorization =
                new TenantAuthorizationService();

            Handler =
                new CreateAppointmentCommandHandler(
                    Appointments,
                    Crm,
                    Availability,
                    TenantContext,
                    Authorization);
        }

        public DateOnly Date { get; } =
            new(2026, 10, 10);

        public TenantContext TenantContext { get; }
        public TestIdentityDbContext Identity { get; }
        public TestServicesDbContext Services { get; }
        public TestWorkforceDbContext Workforce { get; }
        public TestCrmDbContext Crm { get; }
        public TestAppointmentsDbContext Appointments { get; }

        public Guid TenantId { get; }
        public Guid BranchId { get; }
        public Guid ServiceId { get; }
        public Guid CustomerId { get; }
        public Guid StaffId { get; }

        public IStaffScheduleAvailabilityService Schedule { get; }
        public IAppointmentAvailabilityService Availability { get; }
        public IPermissionAuthorizationService Authorization { get; }
        public CreateAppointmentCommandHandler Handler { get; }

        public static async Task<Fixture> CreateAsync(
            int durationMinutes = 60,
            decimal price = 500_000m,
            bool includeBreak = false)
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

            var identityConnection = await OpenAsync();
            var identity =
                new TestIdentityDbContext(
                    new DbContextOptionsBuilder<IdentityDbContext>()
                        .UseSqlite(identityConnection)
                        .Options,
                    tenantContext);

            await identity.Database.EnsureCreatedAsync();

            var tenant = new Tenant(tenantId)
            {
                Name = "Tenant",
                Slug = $"tenant-{tenantId:N}",
                IsActive = true
            };

            var branch = new Branch(
                tenantId,
                "Main",
                isMain: true,
                isActive: true);

            identity.Tenants.Add(tenant);
            identity.Branches.Add(branch);
            await identity.SaveChangesAsync();

            tenantContext.Initialize(
                userId,
                false,
                [tenantId],
                [tenantId],
                tenantId,
                [branch.Id],
                [branch.Id],
                branch.Id);

            var servicesConnection = await OpenAsync();
            var services =
                new TestServicesDbContext(
                    new DbContextOptionsBuilder<ServicesDbContext>()
                        .UseSqlite(servicesConnection)
                        .Options,
                    tenantContext);

            await services.Database.EnsureCreatedAsync();

            var category = new ServiceCategory(
                tenantId,
                "Hair");

            var service = new ServiceEntity(
                tenantId,
                category.Id,
                "Haircut",
                durationMinutes,
                price);

            services.ServiceCategories.Add(category);
            services.Services.Add(service);
            await services.SaveChangesAsync();

            var workforceConnection = await OpenAsync();
            var workforce =
                new TestWorkforceDbContext(
                    new DbContextOptionsBuilder<WorkforceDbContext>()
                        .UseSqlite(workforceConnection)
                        .Options,
                    tenantContext);

            await workforce.Database.EnsureCreatedAsync();

            var staff = new StaffEntity(
                tenantId,
                "Neda",
                "Stylist",
                "09120000001",
                "staff@example.test");

            workforce.Staff.Add(staff);
            workforce.StaffBranches.Add(
                new StaffBranch(
                    tenantId,
                    staff.Id,
                    branch.Id));

            workforce.StaffServices.Add(
                new StaffService(
                    tenantId,
                    staff.Id,
                    service.Id));

            workforce.StaffWeeklyScheduleDays.Add(
                new StaffWeeklyScheduleDay(
                    tenantId,
                    staff.Id,
                    DayOfWeek.Saturday,
                    false,
                    new TimeOnly(9, 0),
                    new TimeOnly(12, 0)));

            await workforce.SaveChangesAsync();

            if (includeBreak)
            {
                var saturday =
                    await workforce.StaffWeeklyScheduleDays
                        .SingleAsync(x =>
                            x.StaffId == staff.Id &&
                            x.DayOfWeek ==
                                DayOfWeek.Saturday);

                workforce.StaffScheduleBreaks.Add(
                    new StaffScheduleBreak(
                        tenantId,
                        saturday.Id,
                        new TimeOnly(10, 0),
                        new TimeOnly(10, 30)));

                await workforce.SaveChangesAsync();
            }

            var crmConnection = await OpenAsync();
            var crm =
                new TestCrmDbContext(
                    new DbContextOptionsBuilder<CrmDbContext>()
                        .UseSqlite(crmConnection)
                        .Options,
                    tenantContext);

            await crm.Database.EnsureCreatedAsync();

            var customer = new Customer(
                tenantId,
                "Sara",
                "Customer",
                "09120000002",
                "09120000002",
                null,
                null,
                null,
                null,
                false);

            crm.Customers.Add(customer);
            await crm.SaveChangesAsync();

            var appointmentsConnection = await OpenAsync();
            var appointments =
                new TestAppointmentsDbContext(
                    new DbContextOptionsBuilder<AppointmentsDbContext>()
                        .UseSqlite(appointmentsConnection)
                        .Options,
                    tenantContext);

            await appointments.Database.EnsureCreatedAsync();

            return new Fixture(
                tenantContext,
                identityConnection,
                identity,
                servicesConnection,
                services,
                workforceConnection,
                workforce,
                crmConnection,
                crm,
                appointmentsConnection,
                appointments,
                tenantId,
                branch.Id,
                service.Id,
                customer.Id,
                staff.Id);
        }

        public async Task<Guid> AddStaffAsync()
        {
            var staff = new StaffEntity(
                TenantId,
                "Other",
                "Stylist",
                Guid.NewGuid().ToString("N"),
                $"{Guid.NewGuid():N}@example.test");

            Workforce.Staff.Add(staff);
            Workforce.StaffBranches.Add(
                new StaffBranch(
                    TenantId,
                    staff.Id,
                    BranchId));

            Workforce.StaffServices.Add(
                new StaffService(
                    TenantId,
                    staff.Id,
                    ServiceId));

            Workforce.StaffWeeklyScheduleDays.Add(
                new StaffWeeklyScheduleDay(
                    TenantId,
                    staff.Id,
                    DayOfWeek.Saturday,
                    false,
                    new TimeOnly(9, 0),
                    new TimeOnly(12, 0)));

            await Workforce.SaveChangesAsync();

            return staff.Id;
        }

        public Task<Result<Guid>> CreateAsync(
            Guid staffId,
            TimeOnly startTime) =>
            Handler.Handle(
                new CreateAppointmentCommand(
                    BranchId,
                    CustomerId,
                    staffId,
                    ServiceId,
                    Date,
                    startTime,
                    null),
                CancellationToken.None);

        public async ValueTask DisposeAsync()
        {
            await Appointments.DisposeAsync();
            await Crm.DisposeAsync();
            await Workforce.DisposeAsync();
            await Services.DisposeAsync();
            await Identity.DisposeAsync();

            await _appointmentsConnection.DisposeAsync();
            await _crmConnection.DisposeAsync();
            await _workforceConnection.DisposeAsync();
            await _servicesConnection.DisposeAsync();
            await _identityConnection.DisposeAsync();
        }

        private static async Task<SqliteConnection> OpenAsync()
        {
            var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();
            return connection;
        }
    }

    private abstract class RowVersionTestDbContext
    {
        protected static void DisableRowVersion(
            ModelBuilder builder)
        {
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

    public sealed class TestIdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantContext tenantContext)
        : IdentityDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DisableRowVersion(builder);
        }

        private static void DisableRowVersion(
            ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var rowVersion = entityType.FindProperty("RowVersion");
                if (rowVersion is not null)
                    rowVersion.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    public sealed class TestServicesDbContext(
        DbContextOptions<ServicesDbContext> options,
        TenantContext tenantContext)
        : ServicesDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DisableRowVersion(builder);
        }

        private static void DisableRowVersion(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var rowVersion = entityType.FindProperty("RowVersion");
                if (rowVersion is not null)
                    rowVersion.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    public sealed class TestWorkforceDbContext(
        DbContextOptions<WorkforceDbContext> options,
        TenantContext tenantContext)
        : WorkforceDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DisableRowVersion(builder);
        }

        private static void DisableRowVersion(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var rowVersion = entityType.FindProperty("RowVersion");
                if (rowVersion is not null)
                    rowVersion.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    public sealed class TestCrmDbContext(
        DbContextOptions<CrmDbContext> options,
        TenantContext tenantContext)
        : CrmDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DisableRowVersion(builder);
        }

        private static void DisableRowVersion(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var rowVersion = entityType.FindProperty("RowVersion");
                if (rowVersion is not null)
                    rowVersion.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    public sealed class TestAppointmentsDbContext(
        DbContextOptions<AppointmentsDbContext> options,
        TenantContext tenantContext)
        : AppointmentsDbContext(options, tenantContext)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DisableRowVersion(builder);
        }

        private static void DisableRowVersion(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var rowVersion = entityType.FindProperty("RowVersion");
                if (rowVersion is not null)
                    rowVersion.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    private sealed class TenantAuthorizationService
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
                    PermissionScopeType.Tenant));

        public Task<PermissionDecision> AuthorizeAsync(
            Guid userId,
            string resource,
            string action,
            PermissionScopeContext resourceContext,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                PermissionDecision.Allow(
                    $"{resource}.{action}",
                    PermissionScopeType.Tenant));

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
                    PermissionScopeType.Tenant.ToString(),
                    null)
            ]);

        public Task<IReadOnlyList<EffectivePermissionDto>>
            GetEffectivePermissionsAsync(
                Guid userId,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
