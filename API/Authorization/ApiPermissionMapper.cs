using POSSystem.Application.Common.Constants;

namespace POSSystem.API.Authorization;

public static class ApiPermissionMapper
{
    private static readonly Dictionary<string, string> ControllerModuleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Categories"] = PermissionModules.Categories,
        ["SubCategories"] = PermissionModules.SubCategories,
        ["Brands"] = PermissionModules.Brands,
        ["Products"] = PermissionModules.Products,
        ["Units"] = PermissionModules.Units,
        ["Menu"] = PermissionModules.Menu,
        ["Orders"] = PermissionModules.Orders,
        ["Kitchen"] = PermissionModules.Orders,
        ["Inventory"] = PermissionModules.Inventory,
        ["Reports"] = PermissionModules.Reports,
        ["Users"] = PermissionModules.Users,
        ["Roles"] = PermissionModules.Roles,
        ["Modules"] = PermissionModules.Roles,
        ["role-permissions"] = PermissionModules.Roles,
        ["Branches"] = PermissionModules.Branches,
        ["Businesses"] = PermissionModules.Businesses,
        ["Countries"] = PermissionModules.Branches,
        ["Recipes"] = PermissionModules.Products
    };

    public static (string Module, string Action)? Resolve(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var metadata = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>();
        if (metadata != null)
            return (metadata.Module, metadata.Action);

        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (path.Contains("/export", StringComparison.OrdinalIgnoreCase))
        {
            var exportModule = ResolveModuleFromPath(path);
            return exportModule == null ? null : (exportModule, PermissionActions.Export);
        }

        if (IsUploadRequest(context, path))
        {
            var uploadModule = ResolveModuleFromPath(path);
            if (uploadModule == null)
                return null;

            var uploadAction = method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                ? PermissionActions.Create
                : PermissionActions.Edit;

            return (uploadModule, uploadAction);
        }

        if (path.Contains("/menu/pos", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/pos", StringComparison.OrdinalIgnoreCase))
        {
            return (PermissionModules.PosBilling, PermissionActions.View);
        }

        var module = ResolveModuleFromPath(path);
        if (module == null)
            return null;

        var action = method.ToUpperInvariant() switch
        {
            "GET" or "HEAD" => PermissionActions.View,
            "POST" => PermissionActions.Create,
            "PUT" or "PATCH" => PermissionActions.Edit,
            "DELETE" => PermissionActions.Delete,
            _ => PermissionActions.View
        };

        return (module, action);
    }

    private static string? ResolveModuleFromPath(string path)
    {
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length < 2 || !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            return null;

        return ControllerModuleMap.TryGetValue(segments[1], out var module) ? module : null;
    }

    private static bool IsUploadRequest(HttpContext context, string path)
    {
        if (path.Contains("/image", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/logo", StringComparison.OrdinalIgnoreCase))
        {
            return context.Request.Method is "POST" or "PUT" or "PATCH";
        }

        return context.Request.HasFormContentType &&
               context.Request.Form.Files.Count > 0 &&
               context.Request.Method is "POST" or "PUT" or "PATCH";
    }
}
