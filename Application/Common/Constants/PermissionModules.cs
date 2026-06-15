namespace POSSystem.Application.Common.Constants;

public static class PermissionModules
{
    public const string Categories = "Categories";
    public const string SubCategories = "SubCategories";
    public const string Brands = "Brands";
    public const string Products = "Products";
    public const string Units = "Units";
    public const string Menu = "Menu";
    public const string Orders = "Orders";
    public const string Inventory = "Inventory";
    public const string Reports = "Reports";
    public const string PosBilling = "POS Billing";
    public const string Users = "Users";
    public const string Roles = "Roles";
    public const string Branches = "Branches";
    public const string Businesses = "Businesses";
    public const string Warehouses = "Warehouses";
    public const string Suppliers = "Suppliers";
    public const string Purchase = "Purchase";
    public const string Stock = "Stock";

    public static readonly IReadOnlyList<string> All =
    [
        Categories,
        SubCategories,
        Brands,
        Products,
        Units,
        Menu,
        Orders,
        Inventory,
        Reports,
        PosBilling,
        Users,
        Roles,
        Branches,
        Businesses,
        Warehouses,
        Suppliers,
        Purchase,
        Stock
    ];
}
