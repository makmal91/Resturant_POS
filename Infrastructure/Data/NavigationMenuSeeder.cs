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
        new("Products", "/products", "🧾", "Products", "Master Data", 8),
        new("Customers", "/customers", "🙋", null, "Master Data", 9),
        new("Suppliers", "/suppliers", "🚚", null, "Master Data", 10),
        new("Units", "/units", "⚖️", null, "Master Data", 11),
        new("Taxes", "/taxes", "💸", null, "Master Data", 12),
        new("Discounts", "/discounts", "🏷️", null, "Master Data", 13),
        new("Inventory", "/inventory", "📦", "Inventory", "Operations", 1),
        new("Orders", "/orders", "📋", "Orders", "Operations", 2)
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
