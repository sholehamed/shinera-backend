using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.System.Workforce.Infrastructure.Persistence.Contexts;

#nullable disable

namespace Modules.System.Workforce.Migrations;

[DbContext(typeof(WorkforceDbContext))]
[Migration("20261006143000_WeeklyScheduleFoundation")]
public partial class WeeklyScheduleFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StaffWeeklyScheduleDays",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DayOfWeek = table.Column<int>(type: "int", nullable: false),
                IsDayOff = table.Column<bool>(type: "bit", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
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
                table.PrimaryKey("PK_StaffWeeklyScheduleDays", x => x.Id);
                table.UniqueConstraint(
                    "AK_StaffWeeklyScheduleDays_Id_TenantId",
                    x => new { x.Id, x.TenantId });
                table.CheckConstraint(
                    "CK_StaffWeeklyScheduleDays_DayOfWeek",
                    "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                table.CheckConstraint(
                    "CK_StaffWeeklyScheduleDays_DayOfWeek",
                    "[DayOfWeek] BETWEEN 0 AND 6");
                table.CheckConstraint(
                    "CK_StaffWeeklyScheduleDays_WorkingHours",
                    "([IsDayOff] = 1 AND [StartTime] IS NULL AND [EndTime] IS NULL) OR ([IsDayOff] = 0 AND [StartTime] IS NOT NULL AND [EndTime] IS NOT NULL AND [StartTime] < [EndTime])");
                table.ForeignKey(
                    name: "FK_StaffWeeklyScheduleDays_Staff_StaffId_TenantId",
                    columns: x => new { x.StaffId, x.TenantId },
                    principalTable: "Staff",
                    principalColumns: new[] { "Id", "TenantId" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "StaffScheduleBreaks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ScheduleDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
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
                table.PrimaryKey("PK_StaffScheduleBreaks", x => x.Id);
                table.CheckConstraint(
                    "CK_StaffScheduleBreaks_TimeRange",
                    "[StartTime] < [EndTime]");
                table.ForeignKey(
                    name: "FK_StaffScheduleBreaks_StaffWeeklyScheduleDays_ScheduleDayId_TenantId",
                    columns: x => new { x.ScheduleDayId, x.TenantId },
                    principalTable: "StaffWeeklyScheduleDays",
                    principalColumns: new[] { "Id", "TenantId" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StaffScheduleBreaks_CreatedAt",
            table: "StaffScheduleBreaks",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_StaffScheduleBreaks_ScheduleDayId_TenantId",
            table: "StaffScheduleBreaks",
            columns: new[] { "ScheduleDayId", "TenantId" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffScheduleBreaks_TenantId_ScheduleDayId_StartTime_EndTime",
            table: "StaffScheduleBreaks",
            columns: new[] { "TenantId", "ScheduleDayId", "StartTime", "EndTime" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StaffWeeklyScheduleDays_CreatedAt",
            table: "StaffWeeklyScheduleDays",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_StaffWeeklyScheduleDays_StaffId_TenantId",
            table: "StaffWeeklyScheduleDays",
            columns: new[] { "StaffId", "TenantId" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffWeeklyScheduleDays_TenantId_StaffId_DayOfWeek",
            table: "StaffWeeklyScheduleDays",
            columns: new[] { "TenantId", "StaffId", "DayOfWeek" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "StaffScheduleBreaks");
        migrationBuilder.DropTable(name: "StaffWeeklyScheduleDays");
    }
}
