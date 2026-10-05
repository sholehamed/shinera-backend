namespace Modules.System.Identity.Application.Authorization;

public static class SystemPermissionCatalog
{
    public static class Modules
    {
        public const string Resource = "modules";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }

    public static class Tenants
    {
        public const string Resource = "tenants";
        public const string List = "list";
        public const string Create = "create";
        public const string Update = "update";
        public const string Delete = "delete";
    }
}
