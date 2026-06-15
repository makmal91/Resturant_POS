using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class NavigationMenuSeeder
{
    private sealed record MenuSeedEntry(
        string Name,
        string? Route,
        string? Icon,
        string? ModuleName,
        string? ParentGroupName,
        int DisplayOrder);

    private static readonly MenuSeedEntry[] DefaultMenus =
    [
        new("Dashboard", null, null, null, null, 1),
        new("Master Data", null, null, null, null, 2),
        new("Operations", null, null, null, null, 3),
        new("Dashboard", "/", "📊", "POS Billing", "Dashboard", 1),
        new("Businesses", "/businesses", "🏬", "Businesses", "Master Data", 1),
        new("Branches", "/branches", "🏢", "Branches", "Master Data", 2),
        new("Users", "/users", "👥", "Users", "Master Data", 3),
        new("Roles", "/roles", "🔐", "Roles", "Master Data", 4),
        new("Menu", "/menu", "🍽️", "Menu", "Master Data", 5),
        new("Categories", "/categories", "🗂️", "Categories", "Master Data", 6),
        new("SubCategories", "/subcategories", "🧩", "SubCategories", "Master Data", 7),
        new("Brands", "/brands", "🏷️", "Brands", "Master Data", 8),
        new("Products", "/products", "🧾", "Products", "Master Data", 9),
        new("Customers", "/customers", "🙋", null, "Master Data", 10),
        new("Suppliers", "/suppliers", "🚚", "Suppliers", "Master Data", 11),
        new("Units", "/units", "⚖️", "Units", "Master Data", 12),
        new("Taxes", "/taxes", "💸", null, "Master Data", 13),
        new("Discounts", "/discounts", "🏷️", null, "Master Data", 14),
        new("Warehouses", "/warehouses", "🏭", "Warehouses", "Master Data", 15),
        new("POS Billing", "/pos", "🏪", "POS Billing", "Operations", 1),
        new("Inventory", "/inventory", "📦", "Inventory", "Operations", 2),
        new("Purchase", "/purchase", "🛒", "Purchase", "Operations", 3),
        new("Stock", "/stock", "📊", "Stock", "Operations", 4),
        new("Orders", "/orders", "📋", "Orders", "Operations", 5)
    ];

    public static async Task SeedDefaultMenusAsync(POSDbContext context, ILogger logger)
    {
        foreach (var group in DefaultMenus.Where(m => m.ParentGroupName == null))
        {
            await SeedGroupAsync(context, logger, group);
        }

        foreach (var item in DefaultMenus.Where(m => m.ParentGroupName != null))
        {
            await SeedItemAsync(context, logger, item);
        }

        await PatchExistingMenusAsync(context, logger);
    }

    private static Task PatchExistingMenusAsync(POSDbContext context, ILogger logger)
    {
        var patches = new[]
        {
            ExecuteRawSeedAsync(
                context,
                logger,
                "Suppliers ModuleName",
                """
                UPDATE [Menus] SET [ModuleName] = N'Suppliers'
                WHERE [Route] = N'/suppliers' AND ([ModuleName] IS NULL OR [ModuleName] = N'');
                """),
            ExecuteRawSeedAsync(
                context,
                logger,
                "Units ModuleName",
                """
                UPDATE [Menus] SET [ModuleName] = N'Units'
                WHERE [Route] = N'/units' AND ([ModuleName] IS NULL OR [ModuleName] = N'');
                """)
        };

        return Task.WhenAll(patches);
    }

    private static Task SeedGroupAsync(POSDbContext context, ILogger logger, MenuSeedEntry group)
    {
        return ExecuteSeedAsync(
            context,
            logger,
            group.Name,
            $"""
            IF NOT EXISTS (
                SELECT 1 FROM [Menus]
                WHERE [Name] = {group.Name} AND [ParentId] IS NULL AND [Route] IS NULL)
            BEGIN
                INSERT INTO [Menus] ([Name], [Route], [Icon], [ModuleName], [ParentId], [DisplayOrder], [IsActive])
                VALUES ({group.Name}, NULL, NULL, NULL, NULL, {group.DisplayOrder}, 1);
            END
            """);
    }

    private static Task SeedItemAsync(POSDbContext context, ILogger logger, MenuSeedEntry item)
    {
        return ExecuteSeedAsync(
            context,
            logger,
            item.Name,
            $"""
            IF NOT EXISTS (SELECT 1 FROM [Menus] WHERE [Route] = {item.Route})
            BEGIN
                INSERT INTO [Menus] ([Name], [Route], [Icon], [ModuleName], [ParentId], [DisplayOrder], [IsActive])
                SELECT
                    {item.Name},
                    {item.Route},
                    {item.Icon},
                    {item.ModuleName},
                    parent.[Id],
                    {item.DisplayOrder},
                    1
                FROM [Menus] AS parent
                WHERE parent.[Name] = {item.ParentGroupName}
                  AND parent.[ParentId] IS NULL
                  AND parent.[Route] IS NULL;
            END
            """);
    }

    private static async Task ExecuteRawSeedAsync(
        POSDbContext context,
        ILogger logger,
        string menuName,
        string sql)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed navigation menu entry {MenuName}.", menuName);
        }
    }

    private static async Task ExecuteSeedAsync(
        POSDbContext context,
        ILogger logger,
        string menuName,
        FormattableString sql)
    {
        try
        {
            await context.Database.ExecuteSqlInterpolatedAsync(sql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed navigation menu entry {MenuName}.", menuName);
        }
    }
}
