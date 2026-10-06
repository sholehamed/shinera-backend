using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;

#nullable disable

namespace Modules.Identity.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20261006090000_BranchWorkspaceFoundation")]
public partial class BranchWorkspaceFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Branches",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                TenantId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                Name = table.Column<string>(
                    type: "nvarchar(200)",
                    maxLength: 200,
                    nullable: false),
                Phone = table.Column<string>(
                    type: "nvarchar(32)",
                    maxLength: 32,
                    nullable: true),
                Address = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: true),
                IsMain = table.Column<bool>(
                    type: "bit",
                    nullable: false),
                IsActive = table.Column<bool>(
                    type: "bit",
                    nullable: false,
                    defaultValue: true),
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
                    "PK_Branches",
                    x => x.Id);

                table.ForeignKey(
                    name: "FK_Branches_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Branches_CreatedAt",
            table: "Branches",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Branches_TenantId_IsActive",
            table: "Branches",
            columns: new[] { "TenantId", "IsActive" });

        migrationBuilder.Sql(
            """
            INSERT INTO [Branches]
                ([Id], [TenantId], [Name], [Phone], [Address], [IsMain], [IsActive],
                 [CreatedBy], [CreatedAt], [CreatedByIp],
                 [LastModifiedBy], [LastModifiedAt], [LastModifiedByIp])
            SELECT
                NEWID(), t.[Id], t.[Name], NULL, NULL, 1, 1,
                t.[CreatedBy], t.[CreatedAt], t.[CreatedByIp],
                NULL, NULL, NULL
            FROM [Tenants] t
            WHERE NOT EXISTS (
                SELECT 1
                FROM [Branches] b
                WHERE b.[TenantId] = t.[Id]
                  AND b.[IsMain] = 1
            );
            """);

        migrationBuilder.CreateIndex(
            name: "UX_Branches_TenantId_Main",
            table: "Branches",
            column: "TenantId",
            unique: true,
            filter: "[IsMain] = 1");

        migrationBuilder.CreateTable(
            name: "BranchMemberships",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                TenantId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                BranchId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                UserId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                IsActive = table.Column<bool>(
                    type: "bit",
                    nullable: false,
                    defaultValue: true),
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
                    "PK_BranchMemberships",
                    x => x.Id);

                table.ForeignKey(
                    name: "FK_BranchMemberships_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                table.ForeignKey(
                    name: "FK_BranchMemberships_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                table.ForeignKey(
                    name: "FK_BranchMemberships_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BranchMemberships_BranchId",
            table: "BranchMemberships",
            column: "BranchId");

        migrationBuilder.CreateIndex(
            name: "IX_BranchMemberships_CreatedAt",
            table: "BranchMemberships",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_BranchMemberships_TenantId_BranchId_UserId",
            table: "BranchMemberships",
            columns: new[] { "TenantId", "BranchId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BranchMemberships_TenantId_UserId_IsActive",
            table: "BranchMemberships",
            columns: new[] { "TenantId", "UserId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_BranchMemberships_UserId",
            table: "BranchMemberships",
            column: "UserId");

        migrationBuilder.Sql(
            """
            INSERT INTO [BranchMemberships]
                ([Id], [TenantId], [BranchId], [UserId], [IsActive],
                 [CreatedBy], [CreatedAt], [CreatedByIp],
                 [LastModifiedBy], [LastModifiedAt], [LastModifiedByIp])
            SELECT
                NEWID(), tm.[TenantId], b.[Id], tm.[UserId], tm.[IsActive],
                tm.[CreatedBy], tm.[CreatedAt], tm.[CreatedByIp],
                tm.[LastModifiedBy], tm.[LastModifiedAt], tm.[LastModifiedByIp]
            FROM [TenantMemberships] tm
            INNER JOIN [Branches] b
                ON b.[TenantId] = tm.[TenantId]
               AND b.[IsMain] = 1
            WHERE NOT EXISTS (
                SELECT 1
                FROM [BranchMemberships] bm
                WHERE bm.[TenantId] = tm.[TenantId]
                  AND bm.[BranchId] = b.[Id]
                  AND bm.[UserId] = tm.[UserId]
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BranchMemberships");

        migrationBuilder.DropTable(
            name: "Branches");
    }
}
