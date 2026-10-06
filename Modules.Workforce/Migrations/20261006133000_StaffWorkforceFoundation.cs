using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.System.Workforce.Infrastructure.Persistence.Contexts;

#nullable disable

namespace Modules.System.Workforce.Migrations;

[DbContext(typeof(WorkforceDbContext))]
[Migration("20261006133000_StaffWorkforceFoundation")]
public partial class StaffWorkforceFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Staff",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
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
                table.PrimaryKey("PK_Staff", x => x.Id);
                table.UniqueConstraint(
                    "AK_Staff_Id_TenantId",
                    x => new { x.Id, x.TenantId });
            });

        migrationBuilder.CreateTable(
            name: "StaffBranches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                table.PrimaryKey("PK_StaffBranches", x => x.Id);
                table.ForeignKey(
                    name: "FK_StaffBranches_Staff_StaffId_TenantId",
                    columns: x => new { x.StaffId, x.TenantId },
                    principalTable: "Staff",
                    principalColumns: new[] { "Id", "TenantId" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "StaffServices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                table.PrimaryKey("PK_StaffServices", x => x.Id);
                table.ForeignKey(
                    name: "FK_StaffServices_Staff_StaffId_TenantId",
                    columns: x => new { x.StaffId, x.TenantId },
                    principalTable: "Staff",
                    principalColumns: new[] { "Id", "TenantId" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Staff_CreatedAt",
            table: "Staff",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Staff_TenantId_IsActive",
            table: "Staff",
            columns: new[] { "TenantId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "UX_Staff_TenantId_UserId",
            table: "Staff",
            columns: new[] { "TenantId", "UserId" },
            unique: true,
            filter: "[UserId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_StaffBranches_CreatedAt",
            table: "StaffBranches",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_StaffBranches_StaffId_TenantId",
            table: "StaffBranches",
            columns: new[] { "StaffId", "TenantId" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffBranches_TenantId_BranchId",
            table: "StaffBranches",
            columns: new[] { "TenantId", "BranchId" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffBranches_TenantId_StaffId_BranchId",
            table: "StaffBranches",
            columns: new[] { "TenantId", "StaffId", "BranchId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StaffServices_CreatedAt",
            table: "StaffServices",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_StaffServices_StaffId_TenantId",
            table: "StaffServices",
            columns: new[] { "StaffId", "TenantId" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffServices_TenantId_ServiceId",
            table: "StaffServices",
            columns: new[] { "TenantId", "ServiceId" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffServices_TenantId_StaffId_ServiceId",
            table: "StaffServices",
            columns: new[] { "TenantId", "StaffId", "ServiceId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "StaffBranches");
        migrationBuilder.DropTable(name: "StaffServices");
        migrationBuilder.DropTable(name: "Staff");
    }
}
