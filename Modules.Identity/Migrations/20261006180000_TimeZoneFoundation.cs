using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

#nullable disable

namespace Modules.System.Identity.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20261006180000_TimeZoneFoundation")]
public partial class TimeZoneFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DefaultTimeZoneId",
            table: "Tenants",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TimeZoneId",
            table: "Branches",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE [Tenants]
            SET [DefaultTimeZoneId] = N'Etc/UTC'
            WHERE [DefaultTimeZoneId] IS NULL OR LTRIM(RTRIM([DefaultTimeZoneId])) = N'';

            UPDATE [Branches]
            SET [TimeZoneId] = N'Etc/UTC'
            WHERE [TimeZoneId] IS NULL OR LTRIM(RTRIM([TimeZoneId])) = N'';
            """);

        migrationBuilder.AlterColumn<string>(
            name: "DefaultTimeZoneId",
            table: "Tenants",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(128)",
            oldMaxLength: 128,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "TimeZoneId",
            table: "Branches",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(128)",
            oldMaxLength: 128,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DefaultTimeZoneId",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "TimeZoneId",
            table: "Branches");
    }
}
