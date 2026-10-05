using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Identity.Migrations;

public partial class AuthorizationPermissionAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Action",
            table: "Permissions",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Code",
            table: "Permissions",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.Sql(
            """
            UPDATE [Permissions]
            SET
                [Code] = LOWER(LTRIM(RTRIM([Code]))),
                [Action] =
                    CASE
                        WHEN CHARINDEX('.', REVERSE(LTRIM(RTRIM([Code])))) > 0
                            THEN LOWER(RIGHT(
                                LTRIM(RTRIM([Code])),
                                CHARINDEX('.', REVERSE(LTRIM(RTRIM([Code])))) - 1))
                        ELSE LOWER(LTRIM(RTRIM([Code])))
                    END;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Action",
            table: "Permissions",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(50)",
            oldMaxLength: 50,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_Code",
            table: "Permissions",
            column: "Code",
            unique: true);

        migrationBuilder.CreateTable(
            name: "PermissionAssignments",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                TenantId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                PermissionId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                SubjectType = table.Column<int>(
                    type: "int",
                    nullable: false),
                SubjectId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                ScopeType = table.Column<int>(
                    type: "int",
                    nullable: false),
                ScopeReferenceId = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: true),
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
                    "PK_PermissionAssignments",
                    x => x.Id);

                table.ForeignKey(
                    name: "FK_PermissionAssignments_Permissions_PermissionId",
                    column: x => x.PermissionId,
                    principalTable: "Permissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);

                table.ForeignKey(
                    name: "FK_PermissionAssignments_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PermissionAssignments_CreatedAt",
            table: "PermissionAssignments",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_PermissionAssignments_PermissionId",
            table: "PermissionAssignments",
            column: "PermissionId");

        migrationBuilder.CreateIndex(
            name: "IX_PermissionAssignments_TenantId_PermissionId_SubjectType_SubjectId_ScopeType_ScopeReferenceId",
            table: "PermissionAssignments",
            columns: new[]
            {
                "TenantId",
                "PermissionId",
                "SubjectType",
                "SubjectId",
                "ScopeType",
                "ScopeReferenceId"
            },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PermissionAssignments_TenantId_SubjectType_SubjectId_IsActive",
            table: "PermissionAssignments",
            columns: new[]
            {
                "TenantId",
                "SubjectType",
                "SubjectId",
                "IsActive"
            });

        // ADR-003 removes Group as an authorization subject.
        // Preserve existing effective access by materializing active group-derived
        // role memberships as direct UserRole rows before switching evaluators.
        migrationBuilder.Sql(
            """
            INSERT INTO [UserRoles]
            (
                [Id],
                [TenantId],
                [UserId],
                [RoleId],
                [CreatedBy],
                [CreatedAt],
                [CreatedByIp],
                [LastModifiedBy],
                [LastModifiedAt],
                [LastModifiedByIp]
            )
            SELECT
                NEWID(),
                ug.[TenantId],
                ug.[UserId],
                gr.[RoleId],
                ug.[CreatedBy],
                ug.[CreatedAt],
                ug.[CreatedByIp],
                ug.[LastModifiedBy],
                ug.[LastModifiedAt],
                ug.[LastModifiedByIp]
            FROM [UserGroups] ug
            INNER JOIN [Groups] g
                ON g.[Id] = ug.[GroupId]
               AND g.[TenantId] = ug.[TenantId]
            INNER JOIN [GroupRoles] gr
                ON gr.[GroupId] = ug.[GroupId]
               AND gr.[TenantId] = ug.[TenantId]
            WHERE g.[IsActive] = 1
              AND gr.[IsDeleted] = 0
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM [UserRoles] ur
                  WHERE ur.[TenantId] = ug.[TenantId]
                    AND ur.[UserId] = ug.[UserId]
                    AND ur.[RoleId] = gr.[RoleId]
              );
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO [PermissionAssignments]
            (
                [Id],
                [TenantId],
                [PermissionId],
                [SubjectType],
                [SubjectId],
                [ScopeType],
                [ScopeReferenceId],
                [IsActive],
                [CreatedBy],
                [CreatedAt],
                [CreatedByIp],
                [LastModifiedBy],
                [LastModifiedAt],
                [LastModifiedByIp]
            )
            SELECT
                NEWID(),
                rp.[TenantId],
                rp.[PermissionId],
                1,
                rp.[RoleId],
                1,
                NULL,
                CAST(1 AS bit),
                rp.[CreatedBy],
                rp.[CreatedAt],
                rp.[CreatedByIp],
                rp.[LastModifiedBy],
                rp.[LastModifiedAt],
                rp.[LastModifiedByIp]
            FROM [RolePermissions] rp
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM [PermissionAssignments] pa
                WHERE pa.[TenantId] = rp.[TenantId]
                  AND pa.[PermissionId] = rp.[PermissionId]
                  AND pa.[SubjectType] = 1
                  AND pa.[SubjectId] = rp.[RoleId]
                  AND pa.[ScopeType] = 1
                  AND pa.[ScopeReferenceId] IS NULL
            );
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO [PermissionAssignments]
            (
                [Id],
                [TenantId],
                [PermissionId],
                [SubjectType],
                [SubjectId],
                [ScopeType],
                [ScopeReferenceId],
                [IsActive],
                [CreatedBy],
                [CreatedAt],
                [CreatedByIp],
                [LastModifiedBy],
                [LastModifiedAt],
                [LastModifiedByIp]
            )
            SELECT
                NEWID(),
                up.[TenantId],
                up.[PermissionId],
                2,
                up.[UserId],
                1,
                NULL,
                CAST(1 AS bit),
                up.[CreatedBy],
                up.[CreatedAt],
                up.[CreatedByIp],
                up.[LastModifiedBy],
                up.[LastModifiedAt],
                up.[LastModifiedByIp]
            FROM [UserPermissions] up
            WHERE up.[IsGranted] = 1
              AND NOT EXISTS
            (
                SELECT 1
                FROM [PermissionAssignments] pa
                WHERE pa.[TenantId] = up.[TenantId]
                  AND pa.[PermissionId] = up.[PermissionId]
                  AND pa.[SubjectType] = 2
                  AND pa.[SubjectId] = up.[UserId]
                  AND pa.[ScopeType] = 1
                  AND pa.[ScopeReferenceId] IS NULL
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PermissionAssignments");

        migrationBuilder.DropIndex(
            name: "IX_Permissions_Code",
            table: "Permissions");

        migrationBuilder.DropColumn(
            name: "Action",
            table: "Permissions");

        migrationBuilder.AlterColumn<string>(
            name: "Code",
            table: "Permissions",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(150)",
            oldMaxLength: 150);
    }
}
