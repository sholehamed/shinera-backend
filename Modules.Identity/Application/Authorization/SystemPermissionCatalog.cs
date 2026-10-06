namespace Modules.System.Identity.Application.Authorization;

public static class SystemPermissionCatalog
{
    public static class Tenants
    {
        public const string Resource = "tenants";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Branches
    {
        public const string Resource = "branches";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Disable = "disable";
        public const string SetMain = "set_main";
    }

    public static class BusinessProfile
    {
        public const string Resource = "business_profile";
        public const string View = "view";
        public const string Update = "update";
    }

    public static class Modules
    {
        public const string Resource = "modules";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Resources
    {
        public const string Resource = "resources";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Users
    {
        public const string Resource = "users";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Roles
    {
        public const string Resource = "roles";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Groups
    {
        public const string Resource = "groups";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Permissions
    {
        public const string Resource = "permissions";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Menus
    {
        public const string Resource = "menus";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Services
    {
        public const string Resource = "services";
        public const string View = "view";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Staff
    {
        public const string Resource = "staff";
        public const string View = "view";
        public const string Create = "create";
        public const string Update = "update";
        public const string AssignBranches = "assign_branches";
        public const string AssignServices = "assign_services";
        public const string ViewSchedule = "view_schedule";
        public const string UpdateSchedule = "update_schedule";
    }

    public static class Dashboard
    {
        public const string Resource = "dashboard";
        public const string View = "view";
    }

    public static readonly string[] WorkspaceOwnerBaselinePermissionKeys =
    [
        Key(Tenants.Resource, Tenants.List),
        Key(Tenants.Resource, Tenants.Update),
        Key(Branches.Resource, Branches.List),
        Key(Branches.Resource, Branches.Create),
        Key(Branches.Resource, Branches.Update),
        Key(Branches.Resource, Branches.Disable),
        Key(Branches.Resource, Branches.SetMain),
        Key(BusinessProfile.Resource, BusinessProfile.View),
        Key(BusinessProfile.Resource, BusinessProfile.Update),
        Key(Users.Resource, Users.List),
        Key(Users.Resource, Users.Create),
        Key(Users.Resource, Users.Update),
        Key(Users.Resource, Users.Delete),
        Key(Roles.Resource, Roles.List),
        Key(Roles.Resource, Roles.Create),
        Key(Roles.Resource, Roles.Update),
        Key(Roles.Resource, Roles.Delete),
        Key(Groups.Resource, Groups.List),
        Key(Groups.Resource, Groups.Create),
        Key(Groups.Resource, Groups.Update),
        Key(Groups.Resource, Groups.Delete),
        Key(Permissions.Resource, Permissions.List),
        Key(Dashboard.Resource, Dashboard.View)
    ];

    public static readonly string[] WorkspaceOwnerPermissionKeys =
    [
        .. WorkspaceOwnerBaselinePermissionKeys,
        Key(Services.Resource, Services.View),
        Key(Services.Resource, Services.Create),
        Key(Services.Resource, Services.Update),
        Key(Services.Resource, Services.Delete),
        Key(Staff.Resource, Staff.View),
        Key(Staff.Resource, Staff.Create),
        Key(Staff.Resource, Staff.Update),
        Key(Staff.Resource, Staff.AssignBranches),
        Key(Staff.Resource, Staff.AssignServices),
        Key(Staff.Resource, Staff.ViewSchedule),
        Key(Staff.Resource, Staff.UpdateSchedule)
    ];

    public static string Key(string resource, string action) =>
        $"{resource.Trim().ToLowerInvariant()}.{action.Trim().ToLowerInvariant()}";
}
