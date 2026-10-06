using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.System.Appointments.Infrastructure.Persistence.Contexts;

#nullable disable

namespace Modules.System.Appointments.Migrations;

[DbContext(typeof(AppointmentsDbContext))]
[Migration("20261006170000_AppointmentCoreFoundation")]
public partial class AppointmentCoreFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Appointments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                table.PrimaryKey("PK_Appointments", x => x.Id);
                table.UniqueConstraint(
                    "AK_Appointments_Id_TenantId",
                    x => new { x.Id, x.TenantId });
                table.CheckConstraint(
                    "CK_Appointments_Price",
                    "[Price] >= 0");
                table.CheckConstraint(
                    "CK_Appointments_Status",
                    "[Status] >= 1 AND [Status] <= 7");
                table.CheckConstraint(
                    "CK_Appointments_TimeRange",
                    "[StartTime] < [EndTime]");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_CreatedAt",
            table: "Appointments",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_TenantId_BranchId_Date",
            table: "Appointments",
            columns: new[] { "TenantId", "BranchId", "Date" });

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_TenantId_CustomerId_Date",
            table: "Appointments",
            columns: new[] { "TenantId", "CustomerId", "Date" });

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_Tenant_Staff_Date_Time",
            table: "Appointments",
            columns: new[] { "TenantId", "StaffId", "Date", "StartTime", "EndTime" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Appointments");
    }
}
