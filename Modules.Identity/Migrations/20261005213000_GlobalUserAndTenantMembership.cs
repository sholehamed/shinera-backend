using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Identity.Migrations;

public partial class GlobalUserAndTenantMembership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TenantMemberships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
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
                table.PrimaryKey("PK_TenantMemberships", x => x.Id);
                table.ForeignKey(
                    name: "FK_TenantMemberships_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TenantMemberships_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TenantMemberships_CreatedAt",
            table: "TenantMemberships",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_TenantMemberships_TenantId_UserId",
            table: "TenantMemberships",
            columns: new[] { "TenantId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TenantMemberships_UserId",
            table: "TenantMemberships",
            column: "UserId");

        migrationBuilder.Sql(
            """
            INSERT INTO [TenantMemberships]
                ([Id], [TenantId], [UserId], [IsActive],
                 [CreatedBy], [CreatedAt], [CreatedByIp],
                 [LastModifiedBy], [LastModifiedAt], [LastModifiedByIp])
            SELECT
                NEWID(),
                u.[TenantId],
                u.[Id],
                CAST(1 AS bit),
                u.[CreatedBy],
                u.[CreatedAt],
                u.[CreatedByIp],
                u.[LastModifiedBy],
                u.[LastModifiedAt],
                u.[LastModifiedByIp]
            FROM [Users] u
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM [TenantMemberships] m
                WHERE m.[TenantId] = u.[TenantId]
                  AND m.[UserId] = u.[Id]
            );
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_Users_Tenants_TenantId",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_TenantId_NormalizedEmail",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_TenantId_NormalizedUserName",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Users");

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedUserName",
            table: "Users",
            column: "NormalizedUserName");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_NormalizedUserName",
            table: "Users");

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Users",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE u
            SET u.[TenantId] = selected.[TenantId]
            FROM [Users] u
            CROSS APPLY
            (
                SELECT TOP (1) m.[TenantId]
                FROM [TenantMemberships] m
                WHERE m.[UserId] = u.[Id]
                ORDER BY m.[CreatedAt], m.[Id]
            ) selected;
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM [Users] WHERE [TenantId] IS NULL)
                THROW 51000, 'Cannot rollback GlobalUserAndTenantMembership because at least one User has no TenantMembership.', 1;
            """);

        migrationBuilder.DropTable(
            name: "TenantMemberships");

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "Users",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_TenantId_NormalizedEmail",
            table: "Users",
            columns: new[] { "TenantId", "NormalizedEmail" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_TenantId_NormalizedUserName",
            table: "Users",
            columns: new[] { "TenantId", "NormalizedUserName" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Tenants_TenantId",
            table: "Users",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
