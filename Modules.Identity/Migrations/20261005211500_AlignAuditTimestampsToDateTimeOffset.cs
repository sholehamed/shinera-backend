using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Identity.Migrations;

public partial class AlignAuditTimestampsToDateTimeOffset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ApiResources_CreatedAt",
            table: "ApiResources");

        migrationBuilder.DropIndex(
            name: "IX_Groups_CreatedAt",
            table: "Groups");

        migrationBuilder.DropIndex(
            name: "IX_GroupRoles_CreatedAt",
            table: "GroupRoles");

        migrationBuilder.DropIndex(
            name: "IX_Menus_CreatedAt",
            table: "Menus");

        migrationBuilder.DropIndex(
            name: "IX_MenuCategories_CreatedAt",
            table: "MenuCategories");

        migrationBuilder.DropIndex(
            name: "IX_Modules_CreatedAt",
            table: "Modules");

        migrationBuilder.DropIndex(
            name: "IX_Permissions_CreatedAt",
            table: "Permissions");

        migrationBuilder.DropIndex(
            name: "IX_PermissionApiResources_CreatedAt",
            table: "PermissionApiResources");

        migrationBuilder.DropIndex(
            name: "IX_PermissionUiResources_CreatedAt",
            table: "PermissionUiResources");

        migrationBuilder.DropIndex(
            name: "IX_Resources_CreatedAt",
            table: "Resources");

        migrationBuilder.DropIndex(
            name: "IX_Roles_CreatedAt",
            table: "Roles");

        migrationBuilder.DropIndex(
            name: "IX_RolePermissions_CreatedAt",
            table: "RolePermissions");

        migrationBuilder.DropIndex(
            name: "IX_Tenants_CreatedAt",
            table: "Tenants");

        migrationBuilder.DropIndex(
            name: "IX_TenantModules_CreatedAt",
            table: "TenantModules");

        migrationBuilder.DropIndex(
            name: "IX_UiResources_CreatedAt",
            table: "UiResources");

        migrationBuilder.DropIndex(
            name: "IX_Users_CreatedAt",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_UserGroups_CreatedAt",
            table: "UserGroups");

        migrationBuilder.DropIndex(
            name: "IX_UserPermissions_CreatedAt",
            table: "UserPermissions");

        migrationBuilder.DropIndex(
            name: "IX_UserRoles_CreatedAt",
            table: "UserRoles");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "ApiResources",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "ApiResources",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Groups",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "Groups",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "GroupRoles",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "DeletedAt",
            table: "GroupRoles",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "GroupRoles",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Menus",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "Menus",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "MenuCategories",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "MenuCategories",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Modules",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "Modules",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Permissions",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "Permissions",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "PermissionApiResources",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "PermissionApiResources",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "PermissionUiResources",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "PermissionUiResources",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Resources",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "Resources",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Roles",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "Roles",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RolePermissions",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "RolePermissions",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Tenants",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "Tenants",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "TenantModules",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "TenantModules",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "UiResources",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "UiResources",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Users",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "Users",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "UserGroups",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "UserGroups",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "UserPermissions",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "UserPermissions",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "UserRoles",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "UserRoles",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "UserTenantAccess",
            type: "datetimeoffset",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedAt",
            table: "UserTenantAccess",
            type: "datetimeoffset",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ApiResources_CreatedAt",
            table: "ApiResources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Groups_CreatedAt",
            table: "Groups",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_GroupRoles_CreatedAt",
            table: "GroupRoles",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Menus_CreatedAt",
            table: "Menus",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_MenuCategories_CreatedAt",
            table: "MenuCategories",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Modules_CreatedAt",
            table: "Modules",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_CreatedAt",
            table: "Permissions",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_PermissionApiResources_CreatedAt",
            table: "PermissionApiResources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_PermissionUiResources_CreatedAt",
            table: "PermissionUiResources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Resources_CreatedAt",
            table: "Resources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Roles_CreatedAt",
            table: "Roles",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_CreatedAt",
            table: "RolePermissions",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_CreatedAt",
            table: "Tenants",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_TenantModules_CreatedAt",
            table: "TenantModules",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_UiResources_CreatedAt",
            table: "UiResources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Users_CreatedAt",
            table: "Users",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_UserGroups_CreatedAt",
            table: "UserGroups",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissions_CreatedAt",
            table: "UserPermissions",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_CreatedAt",
            table: "UserRoles",
            column: "CreatedAt",
            descending: new[] { true });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ApiResources_CreatedAt",
            table: "ApiResources");

        migrationBuilder.DropIndex(
            name: "IX_Groups_CreatedAt",
            table: "Groups");

        migrationBuilder.DropIndex(
            name: "IX_GroupRoles_CreatedAt",
            table: "GroupRoles");

        migrationBuilder.DropIndex(
            name: "IX_Menus_CreatedAt",
            table: "Menus");

        migrationBuilder.DropIndex(
            name: "IX_MenuCategories_CreatedAt",
            table: "MenuCategories");

        migrationBuilder.DropIndex(
            name: "IX_Modules_CreatedAt",
            table: "Modules");

        migrationBuilder.DropIndex(
            name: "IX_Permissions_CreatedAt",
            table: "Permissions");

        migrationBuilder.DropIndex(
            name: "IX_PermissionApiResources_CreatedAt",
            table: "PermissionApiResources");

        migrationBuilder.DropIndex(
            name: "IX_PermissionUiResources_CreatedAt",
            table: "PermissionUiResources");

        migrationBuilder.DropIndex(
            name: "IX_Resources_CreatedAt",
            table: "Resources");

        migrationBuilder.DropIndex(
            name: "IX_Roles_CreatedAt",
            table: "Roles");

        migrationBuilder.DropIndex(
            name: "IX_RolePermissions_CreatedAt",
            table: "RolePermissions");

        migrationBuilder.DropIndex(
            name: "IX_Tenants_CreatedAt",
            table: "Tenants");

        migrationBuilder.DropIndex(
            name: "IX_TenantModules_CreatedAt",
            table: "TenantModules");

        migrationBuilder.DropIndex(
            name: "IX_UiResources_CreatedAt",
            table: "UiResources");

        migrationBuilder.DropIndex(
            name: "IX_Users_CreatedAt",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_UserGroups_CreatedAt",
            table: "UserGroups");

        migrationBuilder.DropIndex(
            name: "IX_UserPermissions_CreatedAt",
            table: "UserPermissions");

        migrationBuilder.DropIndex(
            name: "IX_UserRoles_CreatedAt",
            table: "UserRoles");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "UserTenantAccess",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "UserTenantAccess",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "UserRoles",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "UserRoles",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "UserPermissions",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "UserPermissions",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "UserGroups",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "UserGroups",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "Users",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Users",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "UiResources",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "UiResources",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "TenantModules",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "TenantModules",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "Tenants",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Tenants",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "RolePermissions",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "RolePermissions",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "Roles",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Roles",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "Resources",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Resources",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "PermissionUiResources",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "PermissionUiResources",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "PermissionApiResources",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "PermissionApiResources",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "Permissions",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Permissions",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "Modules",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Modules",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "MenuCategories",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "MenuCategories",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "Menus",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Menus",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "GroupRoles",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "DeletedAt",
            table: "GroupRoles",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "GroupRoles",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "Groups",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Groups",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastModifiedAt",
            table: "ApiResources",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "ApiResources",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "datetimeoffset");

        migrationBuilder.CreateIndex(
            name: "IX_ApiResources_CreatedAt",
            table: "ApiResources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Groups_CreatedAt",
            table: "Groups",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_GroupRoles_CreatedAt",
            table: "GroupRoles",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Menus_CreatedAt",
            table: "Menus",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_MenuCategories_CreatedAt",
            table: "MenuCategories",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Modules_CreatedAt",
            table: "Modules",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_CreatedAt",
            table: "Permissions",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_PermissionApiResources_CreatedAt",
            table: "PermissionApiResources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_PermissionUiResources_CreatedAt",
            table: "PermissionUiResources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Resources_CreatedAt",
            table: "Resources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Roles_CreatedAt",
            table: "Roles",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_CreatedAt",
            table: "RolePermissions",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_CreatedAt",
            table: "Tenants",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_TenantModules_CreatedAt",
            table: "TenantModules",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_UiResources_CreatedAt",
            table: "UiResources",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Users_CreatedAt",
            table: "Users",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_UserGroups_CreatedAt",
            table: "UserGroups",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissions_CreatedAt",
            table: "UserPermissions",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_CreatedAt",
            table: "UserRoles",
            column: "CreatedAt",
            descending: new[] { true });
    }
}
