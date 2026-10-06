using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.System.Crm.Infrastructure.Persistence.Contexts;

#nullable disable

namespace Modules.System.Crm.Migrations;

[DbContext(typeof(CrmDbContext))]
[Migration("20261006160000_CustomerCrmFoundation")]
public partial class CustomerCrmFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Mobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                NormalizedMobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                Gender = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                IsVip = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastModifiedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
                table.UniqueConstraint(
                    "AK_Customers_Id_TenantId",
                    x => new { x.Id, x.TenantId });
            });

        migrationBuilder.CreateTable(
            name: "CustomerNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastModifiedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomerNotes", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomerNotes_Customers_CustomerId_TenantId",
                    columns: x => new { x.CustomerId, x.TenantId },
                    principalTable: "Customers",
                    principalColumns: new[] { "Id", "TenantId" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Customers_CreatedAt",
            table: "Customers",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Customers_TenantId_IsActive",
            table: "Customers",
            columns: new[] { "TenantId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "UX_Customers_TenantId_NormalizedMobile",
            table: "Customers",
            columns: new[] { "TenantId", "NormalizedMobile" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Customers_TenantId_UserId",
            table: "Customers",
            columns: new[] { "TenantId", "UserId" },
            unique: true,
            filter: "[UserId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_CustomerNotes_CreatedAt",
            table: "CustomerNotes",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerNotes_CustomerId_TenantId",
            table: "CustomerNotes",
            columns: new[] { "CustomerId", "TenantId" });

        migrationBuilder.CreateIndex(
            name: "IX_CustomerNotes_TenantId_CustomerId_CreatedAt",
            table: "CustomerNotes",
            columns: new[] { "TenantId", "CustomerId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CustomerNotes");
        migrationBuilder.DropTable(name: "Customers");
    }
}
