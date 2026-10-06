using Application.SharedKernel.Exceptions;
using Application.SharedKernel.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules.System.Crm.Application.Authorization;
using Modules.System.Crm.Application.Features.Customers;
using Modules.System.Crm.Application.Normalization;
using Modules.System.Crm.Domain.Entities;
using Modules.System.Crm.Infrastructure.Persistence.Contexts;
using Modules.System.Crm.Infrastructure.Persistence.Interceptors;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;

namespace Application.Tests.Crm;

public sealed class CustomerCrmTests
{
    [Theory]
    [InlineData("0912 123-4567", "09121234567")]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷", "09121234567")]
    [InlineData("٠٩١٢١٢٣٤٥٦٧", "09121234567")]
    [InlineData("0098 912 123 4567", "+989121234567")]
    public void MobileNormalizer_NormalizesFormattingAndDigits(
        string input,
        string expected)
    {
        Assert.Equal(
            expected,
            CustomerMobileNormalizer.Normalize(input));
    }

    [Fact]
    public async Task CreateCustomer_DoesNotRequireLinkedUser()
    {
        await using var fixture = await CreateFixtureAsync();

        var handler = new CreateCustomerCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var result = await handler.Handle(
            new CreateCustomerCommand(
                "Neda",
                "Ahmadi",
                "۰۹۱۲ ۱۲۳ ۴۵۶۷",
                "neda@example.test",
                new DateOnly(1995, 5, 10),
                "Female",
                null,
                true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var customer = await fixture.Db.Customers
            .AsNoTracking()
            .SingleAsync(x => x.Id == result.Value);

        Assert.Null(customer.UserId);
        Assert.Equal(
            "09121234567",
            customer.NormalizedMobile);
        Assert.Equal(fixture.TenantId, customer.TenantId);
        Assert.True(customer.IsVip);
    }

    [Fact]
    public async Task CreateCustomer_DetectsDuplicateNormalizedMobileWithinTenant()
    {
        await using var fixture = await CreateFixtureAsync();

        var handler = new CreateCustomerCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var first = await handler.Handle(
            CustomerCommand(
                "Neda",
                "0912 123 4567"),
            CancellationToken.None);

        var duplicate = await handler.Handle(
            CustomerCommand(
                "Sara",
                "۰۹۱۲۱۲۳۴۵۶۷"),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal(
            "customer.mobile_duplicate",
            duplicate.Error.Code);
        Assert.Equal(
            ErrorType.Conflict,
            duplicate.Error.Type);
    }

    [Fact]
    public async Task SameMobile_IsAllowedAcrossDifferentTenants()
    {
        await using var fixture = await CreateFixtureAsync();

        fixture.Db.Customers.Add(
            new Customer(
                fixture.TenantId,
                "Neda",
                "One",
                "09121234567",
                "09121234567",
                null,
                null,
                null,
                null,
                false));

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.Customers.Add(
                new Customer(
                    Guid.NewGuid(),
                    "Neda",
                    "Two",
                    "09121234567",
                    "09121234567",
                    null,
                    null,
                    null,
                    null,
                    false));

            await fixture.Db.SaveChangesAsync();
        }

        using (fixture.TenantContext.DisableFilter())
        {
            Assert.Equal(
                2,
                await fixture.Db.Customers.CountAsync(
                    x => x.NormalizedMobile ==
                        "09121234567"));
        }
    }

    [Fact]
    public async Task TenantFilter_HidesForeignCustomersAndNotes()
    {
        await using var fixture = await CreateFixtureAsync();

        var foreignTenantId = Guid.NewGuid();
        var foreignCustomer = new Customer(
            foreignTenantId,
            "Foreign",
            "Customer",
            "09120000001",
            "09120000001",
            null,
            null,
            null,
            null,
            false);

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.Customers.Add(foreignCustomer);
            fixture.Db.CustomerNotes.Add(
                new CustomerNote(
                    foreignTenantId,
                    foreignCustomer.Id,
                    "Hidden note"));

            await fixture.Db.SaveChangesAsync();
        }

        Assert.Equal(
            0,
            await fixture.Db.Customers.CountAsync());

        Assert.Equal(
            0,
            await fixture.Db.CustomerNotes.CountAsync());
    }

    [Fact]
    public async Task CustomerSearch_IsTenantScopedAndMatchesNameMobileEmail()
    {
        await using var fixture = await CreateFixtureAsync();

        await fixture.AddCustomerAsync(
            "Neda",
            "Ahmadi",
            "09121234567",
            "neda@example.test");

        await fixture.AddCustomerAsync(
            "Sara",
            "Karimi",
            "09351234567",
            "sara@example.test");

        var handler = new CustomerSearchQueryHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var byName = await handler.Handle(
            new CustomerSearchQuery
            {
                Search = "Neda"
            },
            CancellationToken.None);

        var byMobile = await handler.Handle(
            new CustomerSearchQuery
            {
                Search = "۰۹۱۲ ۱۲۳ ۴۵۶۷"
            },
            CancellationToken.None);

        var byEmail = await handler.Handle(
            new CustomerSearchQuery
            {
                Search = "sara@example.test"
            },
            CancellationToken.None);

        Assert.Equal(
            "Neda",
            Assert.Single(byName.Data).FirstName);

        Assert.Equal(
            "Neda",
            Assert.Single(byMobile.Data).FirstName);

        Assert.Equal(
            "Sara",
            Assert.Single(byEmail.Data).FirstName);
    }

    [Fact]
    public async Task AddNote_StoresInternalCustomerNoteAndDetailsReturnsIt()
    {
        await using var fixture = await CreateFixtureAsync();

        var customer = await fixture.AddCustomerAsync();

        var addNote = new AddCustomerNoteCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var noteResult = await addNote.Handle(
            new AddCustomerNoteCommand(
                customer.Id,
                "Prefers morning appointments"),
            CancellationToken.None);

        Assert.True(noteResult.IsSuccess);

        var details = new CustomerDetailsQueryHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var result = await details.Handle(
            new CustomerDetailsQuery(customer.Id),
            CancellationToken.None);

        var note = Assert.Single(result.NoteEntries);
        Assert.Equal(
            "Prefers morning appointments",
            note.Content);
    }

    [Fact]
    public async Task AddNote_CannotTargetForeignTenantCustomer()
    {
        await using var fixture = await CreateFixtureAsync();

        var foreignTenantId = Guid.NewGuid();
        var foreignCustomer = new Customer(
            foreignTenantId,
            "Foreign",
            "Customer",
            "09120000002",
            "09120000002",
            null,
            null,
            null,
            null,
            false);

        using (fixture.TenantContext.DisableFilter())
        {
            fixture.Db.Customers.Add(foreignCustomer);
            await fixture.Db.SaveChangesAsync();
        }

        var handler = new AddCustomerNoteCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            fixture.TenantAuthorization);

        var result = await handler.Handle(
            new AddCustomerNoteCommand(
                foreignCustomer.Id,
                "Must not be written"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "customer.not_found",
            result.Error.Code);
    }

    [Fact]
    public async Task CreateCustomer_WithBranchScope_IsDenied()
    {
        await using var fixture = await CreateFixtureAsync();

        var handler = new CreateCustomerCommandHandler(
            fixture.Db,
            fixture.TenantContext,
            new StubAuthorizationService(
                PermissionScopeType.Branch));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => handler.Handle(
                CustomerCommand(
                    "Denied",
                    "09120000003"),
                CancellationToken.None));
    }

    [Fact]
    public async Task CustomerWithLinkedUser_IsSupported()
    {
        await using var fixture = await CreateFixtureAsync();

        var userId = Guid.NewGuid();

        var customer = new Customer(
            fixture.TenantId,
            "Linked",
            "Customer",
            "09120000006",
            "09120000006",
            null,
            null,
            null,
            null,
            false,
            userId);

        fixture.Db.Customers.Add(customer);
        await fixture.Db.SaveChangesAsync();

        var stored = await fixture.Db.Customers
            .AsNoTracking()
            .SingleAsync(x => x.Id == customer.Id);

        Assert.Equal(userId, stored.UserId);
    }

    [Fact]
    public async Task Validator_RejectsMobileWithoutDigits()
    {
        var validator =
            new CreateCustomerCommandValidator();

        var result = await validator.ValidateAsync(
            CustomerCommand(
                "Invalid",
                "+"));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            x => x.PropertyName ==
                nameof(CreateCustomerCommand.Mobile));
    }

    [Fact]
    public async Task CustomerCanBeLinkedToUserButLinkIsTenantUnique()
    {
        await using var fixture = await CreateFixtureAsync();

        var userId = Guid.NewGuid();

        fixture.Db.Customers.AddRange(
            new Customer(
                fixture.TenantId,
                "First",
                "Customer",
                "09120000004",
                "09120000004",
                null,
                null,
                null,
                null,
                false,
                userId),
            new Customer(
                fixture.TenantId,
                "Second",
                "Customer",
                "09120000005",
                "09120000005",
                null,
                null,
                null,
                null,
                false,
                userId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public void CustomerSearchDto_DoesNotExposeInternalNotes()
    {
        var properties =
            typeof(CustomerListItemDto)
                .GetProperties()
                .Select(x => x.Name)
                .ToArray();

        Assert.DoesNotContain(
            nameof(Customer.Notes),
            properties);

        Assert.DoesNotContain(
            nameof(Customer.NoteEntries),
            properties);
    }

    private static CreateCustomerCommand CustomerCommand(
        string firstName,
        string mobile) =>
        new(
            firstName,
            "Customer",
            mobile,
            null,
            null,
            null,
            null,
            false);

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
            new DbContextOptionsBuilder<CrmDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(
                    new CrmTenantSaveChangesInterceptor(
                        tenantContext))
                .Options;

        var db = new TestCrmDbContext(
            options,
            tenantContext);

        await db.Database.EnsureCreatedAsync();

        return new Fixture(
            connection,
            db,
            tenantContext,
            tenantId);
    }

    private sealed class TestCrmDbContext(
        DbContextOptions<CrmDbContext> options,
        TenantContext tenantContext)
        : CrmDbContext(options, tenantContext)
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
        CrmDbContext db,
        TenantContext tenantContext,
        Guid tenantId)
        : IAsyncDisposable
    {
        public CrmDbContext Db { get; } = db;
        public TenantContext TenantContext { get; } =
            tenantContext;
        public Guid TenantId { get; } = tenantId;

        public IPermissionAuthorizationService
            TenantAuthorization { get; } =
            new StubAuthorizationService(
                PermissionScopeType.Tenant);

        public async Task<Customer> AddCustomerAsync(
            string firstName = "Neda",
            string lastName = "Customer",
            string mobile = "09121234567",
            string? email = null)
        {
            var customer = new Customer(
                TenantId,
                firstName,
                lastName,
                mobile,
                CustomerMobileNormalizer.Normalize(mobile),
                email,
                null,
                null,
                null,
                false);

            Db.Customers.Add(customer);
            await Db.SaveChangesAsync();

            return customer;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
