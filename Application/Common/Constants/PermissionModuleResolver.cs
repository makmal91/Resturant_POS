namespace POSSystem.Application.Common.Constants;

/// <summary>
/// Resolves permission module names across API routes, DB module keys, and display names.
/// </summary>
public static class PermissionModuleResolver
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CashFlow"] = PermissionModules.CashFlow,
        ["CashFlow.Ledger"] = PermissionModules.CashFlow,
        ["CashFlow.Summary"] = PermissionModules.CashFlow,
        ["StockReports.ByUnit"] = PermissionModules.StockReports,
        ["PartyLedger"] = PermissionModules.PartyLedger,
        ["PartyLedger.PaySupplier"] = PermissionModules.PartyLedger,
        ["PartyLedger.CustomerLedger"] = PermissionModules.PartyLedger,
        ["PartyLedger.SupplierLedger"] = PermissionModules.PartyLedger,
        ["POS"] = PermissionModules.PosBilling,
        ["PosBilling"] = PermissionModules.PosBilling,
        ["Orders"] = PermissionModules.Orders,
        ["Sales"] = PermissionModules.Sales,
        ["Purchases"] = PermissionModules.Purchase,
        ["Invoices"] = PermissionModules.Sales,
        ["User Roles"] = PermissionModules.Roles,
        ["expensecategories"] = PermissionModules.ExpenseCategories,
        ["codeseq"] = PermissionModules.CodeSequences,
        ["settings"] = PermissionModules.SystemSettings,
    };

    public static string Normalize(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return string.Empty;

        var trimmed = moduleName.Trim();
        if (Aliases.TryGetValue(trimmed, out var alias))
            return alias;

        if (PermissionModules.All.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            return PermissionModules.All.First(m => string.Equals(m, trimmed, StringComparison.OrdinalIgnoreCase));

        var prefix = trimmed.Split('.')[0];
        if (Aliases.TryGetValue(prefix, out var prefixAlias))
            return prefixAlias;

        return trimmed;
    }

    public static bool Matches(string storedModuleName, string requestedModule)
    {
        if (string.IsNullOrWhiteSpace(storedModuleName) || string.IsNullOrWhiteSpace(requestedModule))
            return false;

        if (string.Equals(storedModuleName, requestedModule, StringComparison.OrdinalIgnoreCase))
            return true;

        var normalizedStored = Normalize(storedModuleName);
        var normalizedRequested = Normalize(requestedModule);

        return string.Equals(normalizedStored, normalizedRequested, StringComparison.OrdinalIgnoreCase);
    }
}
