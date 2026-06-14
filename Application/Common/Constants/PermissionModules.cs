namespace POSSystem.Application.Common.Constants;

public static class PermissionModules
{
    public const string Categories = "Categories";
    public const string Products = "Products";
    public const string Orders = "Orders";
    public const string Inventory = "Inventory";
    public const string Reports = "Reports";
    public const string PosBilling = "POS Billing";
    public const string Users = "Users";

    public static readonly IReadOnlyList<string> All =
    [
        Categories,
        Products,
        Orders,
        Inventory,
        Reports,
        PosBilling,
        Users
    ];
}
