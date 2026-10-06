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

    public static class Dashboard
    {
        public const string Resource = "dashboard";
        public const string View = "view";
    }

    public static string Key(string resource, string action) =>
        $"{resource.Trim().ToLowerInvariant()}.{action.Trim().ToLowerInvariant()}";
}
