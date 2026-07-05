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
        ["Barcode"] = PermissionModules.Barcodes,
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
        ["Countries"] = PermissionModules.Countries,
        ["Recipes"] = PermissionModules.Products,
        ["Warehouses"] = PermissionModules.Warehouses,
        ["Suppliers"] = PermissionModules.Suppliers,
        ["Purchase"] = PermissionModules.Purchase,
        ["opening-stock"] = PermissionModules.OpeningStock,
        ["stock-transfer"] = PermissionModules.StockTransfer,
        ["Stock"] = PermissionModules.Stock,
        ["Sales"] = PermissionModules.Sales,
        ["Customers"] = PermissionModules.Customers,
        ["Expenses"] = PermissionModules.Expenses,
        ["expense-categories"] = PermissionModules.ExpenseCategories,
        ["CashFlow"] = PermissionModules.CashFlow,
        ["code-sequences"] = PermissionModules.CodeSequences,
        ["codes"] = PermissionModules.CodeSequences,
        ["ledger"] = PermissionModules.PartyLedger,
        ["payments"] = PermissionModules.PartyLedger,
        ["currencies"] = PermissionModules.SystemSettings,
        ["cashflow"] = PermissionModules.CashFlow,
        ["accounting"] = PermissionModules.AccountLedger,
        ["Dashboard"] = PermissionModules.Dashboard,
    };

    public static (string Module, string Action)? Resolve(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var metadata = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>();
        if (metadata != null)
            return (metadata.Module, metadata.Action);

        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Active lookup lists are shared across POS, Purchase, Stock, and Sales modules.
        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
            (path.Contains("/warehouses/active", StringComparison.OrdinalIgnoreCase) ||
             path.Contains("/suppliers/active", StringComparison.OrdinalIgnoreCase) ||
             path.Contains("/codes/preview", StringComparison.OrdinalIgnoreCase) ||
             path.Contains("/api/masters/", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var mastersModule = ResolveMastersModuleFromPath(path);
        if (mastersModule != null)
        {
            var mastersAction = method.ToUpperInvariant() switch
            {
                "GET" or "HEAD" => PermissionActions.View,
                "POST" => PermissionActions.Create,
                "PUT" or "PATCH" => PermissionActions.Edit,
                "DELETE" => PermissionActions.Delete,
                _ => PermissionActions.View
            };
            return (mastersModule, mastersAction);
        }

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

        var posBillingMapping = ResolvePosBillingEndpoint(path, method);
        if (posBillingMapping != null)
            return posBillingMapping;

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

    /// <summary>
    /// When POS Billing is required, also accept Sales or Customers module rights
    /// (for custom roles that manage invoices/customers without an explicit POS grant).
    /// </summary>
    public static IReadOnlyList<string> GetAlternateModules(string module) =>
        string.Equals(module, PermissionModules.PosBilling, StringComparison.OrdinalIgnoreCase)
            ? [PermissionModules.Sales, PermissionModules.Customers, PermissionModules.PartyLedger]
            : Array.Empty<string>();

    private static (string Module, string Action)? ResolvePosBillingEndpoint(string path, string method)
    {
        if (!IsPosBillingApiEndpoint(path, method))
            return null;

        var action = method.ToUpperInvariant() switch
        {
            "GET" or "HEAD" => PermissionActions.View,
            "POST" => PermissionActions.Create,
            "PUT" or "PATCH" => PermissionActions.Edit,
            "DELETE" => PermissionActions.Delete,
            _ => PermissionActions.View
        };

        return (PermissionModules.PosBilling, action);
    }

    private static bool IsPosBillingApiEndpoint(string path, string method) =>
        IsPosCustomerEndpoint(path) || IsPosSalesEndpoint(path, method);

    private static bool IsPosCustomerEndpoint(string path) =>
        path.Contains("/customers/walk-in", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("/customers/quick-create", StringComparison.OrdinalIgnoreCase);

    private static bool IsPosSalesEndpoint(string path, string method)
    {
        if (!path.Contains("/api/sales/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.Contains("/product/barcode/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/products/search", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/customers/search", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/held", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            path.EndsWith("/hold", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            path.EndsWith("/invoice", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
            !method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length == 4 &&
               segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) &&
               segments[1].Equals("sales", StringComparison.OrdinalIgnoreCase) &&
               segments[2].Equals("invoice", StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(segments[3], out _);
    }

    private static string? ResolveModuleFromPath(string path)
    {
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length < 2 || !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            return null;

        return ControllerModuleMap.TryGetValue(segments[1], out var module) ? module : null;
    }

    private static string? ResolveMastersModuleFromPath(string path)
    {
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var mastersIndex = Array.FindIndex(
            segments,
            s => s.Equals("masters", StringComparison.OrdinalIgnoreCase));

        if (mastersIndex < 0 || mastersIndex + 1 >= segments.Length)
            return null;

        return segments[mastersIndex + 1].ToLowerInvariant() switch
        {
            "size" => PermissionModules.Sizes,
            "color" => PermissionModules.Colors,
            "expense-category" => PermissionModules.ExpenseCategories,
            "country" => PermissionModules.Countries,
            "city" => PermissionModules.Cities,
            _ => null
        };
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
