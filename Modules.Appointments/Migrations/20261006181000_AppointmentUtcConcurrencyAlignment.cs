using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.System.Appointments.Infrastructure.Persistence.Contexts;

#nullable disable

namespace Modules.System.Appointments.Migrations;

[DbContext(typeof(AppointmentsDbContext))]
[Migration("20261006181000_AppointmentUtcConcurrencyAlignment")]
public partial class AppointmentUtcConcurrencyAlignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "StartUtc",
            table: "Appointments",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "EndUtc",
            table: "Appointments",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TimeZoneId",
            table: "Appointments",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE [Appointments]
            SET
                [StartUtc] = TODATETIMEOFFSET(
                    DATEADD(
                        SECOND,
                        DATEDIFF(
                            SECOND,
                            CAST('00:00:00' AS time),
                            [StartTime]),
                        CAST([Date] AS datetime2)),
                    '+00:00'),
                [EndUtc] = TODATETIMEOFFSET(
                    DATEADD(
                        SECOND,
                        DATEDIFF(
                            SECOND,
                            CAST('00:00:00' AS time),
                            [EndTime]),
                        CAST([Date] AS datetime2)),
                    '+00:00'),
                [TimeZoneId] = N'Etc/UTC'
            WHERE [StartUtc] IS NULL
               OR [EndUtc] IS NULL
               OR [TimeZoneId] IS NULL;
            """);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "StartUtc",
            table: "Appointments",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "EndUtc",
            table: "Appointments",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "TimeZoneId",
            table: "Appointments",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(128)",
            oldMaxLength: 128,
            oldNullable: true);

        migrationBuilder.DropIndex(
            name: "IX_Appointments_Tenant_Staff_Date_Time",
            table: "Appointments");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_Tenant_Staff_Date_UtcTime",
            table: "Appointments",
            columns: new[]
            {
                "TenantId",
                "StaffId",
                "Date",
                "StartUtc",
                "EndUtc"
            });

        migrationBuilder.AddCheckConstraint(
            name: "CK_Appointments_UtcTimeRange",
            table: "Appointments",
            sql: "[StartUtc] < [EndUtc]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Appointments_UtcTimeRange",
            table: "Appointments");

        migrationBuilder.DropIndex(
            name: "IX_Appointments_Tenant_Staff_Date_UtcTime",
            table: "Appointments");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_Tenant_Staff_Date_Time",
            table: "Appointments",
            columns: new[]
            {
                "TenantId",
                "StaffId",
                "Date",
                "StartTime",
                "EndTime"
            });

        migrationBuilder.DropColumn(
            name: "StartUtc",
            table: "Appointments");

        migrationBuilder.DropColumn(
            name: "EndUtc",
            table: "Appointments");

        migrationBuilder.DropColumn(
            name: "TimeZoneId",
            table: "Appointments");
    }
}
