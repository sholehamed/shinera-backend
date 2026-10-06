using Application.SharedKernel.Models;
using Infrastructure.SharedKernel.Concurrency;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Modules.System.Appointments.Application.Availability;
using Modules.System.Appointments.Application.Features.Appointments;
using Modules.System.Appointments.Infrastructure.Persistence.Contexts;
using Modules.System.Crm.Domain.Entities;
using Modules.System.Crm.Infrastructure.Persistence.Contexts;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;

namespace IntegrationTests;

public sealed class AppointmentConcurrencySqlServerTests
{
    [Fact]
    public async Task SameStaffSameInterval_ConcurrentRequests_ExactlyOneSucceeds()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "SHINERA_TEST_SQLSERVER");

        // Local developers can run the regular integration suite without
        // requiring SQL Server. CI always provides this value.
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        await WaitForSqlServerAsync(connectionString);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 10, 10);

        var setupTenantContext =
            CreateTenantContext(
                tenantId,
                userId,
                branchId);

        var appointmentOptions =
            new DbContextOptionsBuilder<AppointmentsDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        await using (var setup =
            new AppointmentsDbContext(
                appointmentOptions,
                setupTenantContext))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.EnsureCreatedAsync();

            await setup.Database.ExecuteSqlInterpolatedAsync(
                $"""
                CREATE TABLE [Staff]
                (
                    [Id] uniqueidentifier NOT NULL,
                    [TenantId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_Staff_BookingLock] PRIMARY KEY ([Id])
                );

                CREATE INDEX [IX_Staff_BookingLock_Tenant_Staff]
                    ON [Staff] ([TenantId], [Id]);

                INSERT INTO [Staff] ([Id], [TenantId])
                VALUES ({staffId}, {tenantId});
                """);
        }

        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var first = ExecuteBookingAsync(
            connectionString,
            "a",
            tenantId,
            userId,
            branchId,
            staffId,
            serviceId,
            date,
            startGate.Task);

        var second = ExecuteBookingAsync(
            connectionString,
            "b",
            tenantId,
            userId,
            branchId,
            staffId,
            serviceId,
            date,
            startGate.Task);

        startGate.SetResult();

        var results = await Task.WhenAll(
            first,
            second);

        Assert.Single(results.Where(x => x.IsSuccess));

        var conflict = Assert.Single(
            results.Where(x => x.IsFailure));

        Assert.Equal(
            "appointment.conflict",
            conflict.Error.Code);

        var verificationContext =
            CreateTenantContext(
                tenantId,
                userId,
                branchId);

        await using var verification =
            new AppointmentsDbContext(
                appointmentOptions,
                verificationContext);

        Assert.Equal(
            1,
            await verification.Appointments.CountAsync());
    }

    private static async Task<Result<Guid>> ExecuteBookingAsync(
        string appointmentConnectionString,
        string suffix,
        Guid tenantId,
        Guid userId,
        Guid branchId,
        Guid staffId,
        Guid serviceId,
        DateOnly date,
        Task startSignal)
    {
        var tenantContext =
            CreateTenantContext(
                tenantId,
                userId,
                branchId);

        var appointmentOptions =
            new DbContextOptionsBuilder<AppointmentsDbContext>()
                .UseSqlServer(appointmentConnectionString)
                .Options;

        await using var appointments =
            new AppointmentsDbContext(
                appointmentOptions,
                tenantContext);

        var crmConnectionString =
            WithDatabase(
                appointmentConnectionString,
                $"shinera_concurrency_crm_{suffix}_{Guid.NewGuid():N}");

        var crmOptions =
            new DbContextOptionsBuilder<CrmDbContext>()
                .UseSqlServer(crmConnectionString)
                .Options;

        await using var crm =
            new CrmDbContext(
                crmOptions,
                tenantContext);

        await crm.Database.EnsureCreatedAsync();

        var customer = new Customer(
            tenantId,
            "Concurrent",
            "Customer",
            $"09{Guid.NewGuid():N}"[..11],
            Guid.NewGuid().ToString("N"),
            null,
            null,
            null,
            null,
            false);

        crm.Customers.Add(customer);
        await crm.SaveChangesAsync();

        var handler =
            new CreateAppointmentCommandHandler(
                appointments,
                crm,
                new SlowFixedAvailabilityService(
                    date),
                tenantContext,
                new AllowTenantAuthorizationService(),
                new SqlServerStaffBookingConcurrencyGuard());

        await startSignal;

        var result = await handler.Handle(
            new CreateAppointmentCommand(
                branchId,
                customer.Id,
                staffId,
                serviceId,
                date,
                new TimeOnly(9, 0),
                null),
            CancellationToken.None);

        await crm.Database.EnsureDeletedAsync();

        return result;
    }

    private static TenantContext CreateTenantContext(
        Guid tenantId,
        Guid userId,
        Guid branchId)
    {
        var context = new TenantContext();

        context.Initialize(
            userId,
            false,
            [tenantId],
            [tenantId],
            tenantId,
            [branchId],
            [branchId],
            branchId);

        return context;
    }

    private static string WithDatabase(
        string connectionString,
        string database)
    {
        var builder =
            new SqlConnectionStringBuilder(
                connectionString)
            {
                InitialCatalog = database
            };

        return builder.ConnectionString;
    }

    private static async Task WaitForSqlServerAsync(
        string connectionString)
    {
        var builder =
            new SqlConnectionStringBuilder(
                connectionString)
            {
                InitialCatalog = "master"
            };

        Exception? lastError = null;

        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                await using var connection =
                    new SqlConnection(
                        builder.ConnectionString);

                await connection.OpenAsync();
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                await Task.Delay(
                    TimeSpan.FromSeconds(1));
            }
        }

        throw new InvalidOperationException(
            "SQL Server did not become ready for the concurrency integration test.",
            lastError);
    }

    private sealed class SlowFixedAvailabilityService(
        DateOnly date)
        : IAppointmentAvailabilityService
    {
        public Task<Result<IReadOnlyList<TimeOnly>>>
            GetAvailableSlotsAsync(
                Guid branchId,
                Guid serviceId,
                Guid? staffId,
                DateOnly requestedDate,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task<Result<AppointmentSlotValidation>>
            ValidateSlotAsync(
                Guid branchId,
                Guid serviceId,
                Guid staffId,
                DateOnly requestedDate,
                TimeOnly startTime,
                CancellationToken cancellationToken = default)
        {
            Assert.Equal(date, requestedDate);
            Assert.Equal(
                new TimeOnly(9, 0),
                startTime);

            // Hold the first transaction long enough for the second request
            // to contend on the same Staff row lock.
            await Task.Delay(
                TimeSpan.FromMilliseconds(300),
                cancellationToken);

            return Result<AppointmentSlotValidation>.Success(
                new AppointmentSlotValidation(
                    new TimeOnly(10, 0),
                    new DateTimeOffset(
                        2026, 10, 10, 5, 30, 0, TimeSpan.Zero),
                    new DateTimeOffset(
                        2026, 10, 10, 6, 30, 0, TimeSpan.Zero),
                    "Asia/Tehran",
                    850_000m));
        }
    }

    private sealed class AllowTenantAuthorizationService
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
            Task.FromResult<IReadOnlyList<EffectivePermissionDto>>(
                []);
    }
}
