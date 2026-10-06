using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

#nullable disable

namespace Modules.Identity.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20261006110000_RegistrationBusinessProfileFoundation")]
public partial class RegistrationBusinessProfileFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Phone",
            table: "Users",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users");

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users",
            column: "NormalizedEmail",
            unique: true);

        migrationBuilder.CreateTable(
            name: "BusinessProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                TenantId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                DisplayName = table.Column<string>(
                    type: "nvarchar(200)",
                    maxLength: 200,
                    nullable: false),
                BusinessType = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false),
                Mode = table.Column<int>(
                    type: "int",
                    nullable: false),
                Phone = table.Column<string>(
                    type: "nvarchar(32)",
                    maxLength: 32,
                    nullable: true),
                Email = table.Column<string>(
                    type: "nvarchar(256)",
                    maxLength: 256,
                    nullable: true),
                City = table.Column<string>(
                    type: "nvarchar(150)",
                    maxLength: 150,
                    nullable: true),
                Address = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: true),
                LogoId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: true),
                Description = table.Column<string>(
                    type: "nvarchar(1000)",
                    maxLength: 1000,
                    nullable: true),
                CreatedBy = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "datetimeoffset",
                    nullable: false),
                CreatedByIp = table.Column<string>(
                    type: "nvarchar(45)",
                    maxLength: 45,
                    nullable: true),
                LastModifiedBy = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: true),
                LastModifiedAt = table.Column<DateTimeOffset>(
                    type: "datetimeoffset",
                    nullable: true),
                LastModifiedByIp = table.Column<string>(
                    type: "nvarchar(45)",
                    maxLength: 45,
                    nullable: true),
                RowVersion = table.Column<byte[]>(
                    type: "rowversion",
                    rowVersion: true,
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_BusinessProfiles",
                    x => x.Id);

                table.ForeignKey(
                    name: "FK_BusinessProfiles_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessProfiles_CreatedAt",
            table: "BusinessProfiles",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessProfiles_TenantId",
            table: "BusinessProfiles",
            column: "TenantId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BusinessProfiles");

        migrationBuilder.DropIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "Phone",
            table: "Users");

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users",
            column: "NormalizedEmail");
    }
}
