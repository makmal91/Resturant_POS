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
        new("Accounts", null, null, null, null, 4),
        new("Dashboard", "/", "D", "Dashboard", "Dashboard", 1),
        new("Businesses", "/businesses", "B", "Businesses", "Master Data", 1),
        new("Branches", "/branches", "Br", "Branches", "Master Data", 2),
        new("Users", "/users", "U", "Users", "Master Data", 3),
        new("User Roles", "/roles", "R", "Roles", "Master Data", 4),
        new("Menu", "/menu", "M", "Menu", "Master Data", 5),
        new("Categories", "/categories", "C", "Categories", "Master Data", 6),
        new("SubCategories", "/subcategories", "SC", "SubCategories", "Master Data", 7),
        new("Brands", "/brands", "Bn", "Brands", "Master Data", 8),
        new("Products", "/products", "P", "Products", "Master Data", 9),
        new("Customers", "/customers", "Cu", null, "Master Data", 10),
        new("Suppliers", "/suppliers", "Su", "Suppliers", "Master Data", 11),
        new("Units", "/units", "Un", "Units", "Master Data", 12),
        new("Taxes", "/taxes", "T", null, "Master Data", 13),
        new("Discounts", "/discounts", "Di", null, "Master Data", 14),
        new("Warehouses", "/warehouses", "W", "Warehouses", "Master Data", 15),
        new("Settings", null, null, null, null, 5),
        new("System Settings", "/settings", "settings", "System Settings", "Settings", 1),
        new("Code Sequences", "/settings/code-sequences", "codeseq", "Code Sequences", "Settings", 2),
        new("Countries", "/settings/countries", "countries", "Countries", "Settings", 3),
        new("Cities", "/settings/cities", "cities", "Cities", "Settings", 4),
        new("Sizes", "/settings/sizes", "sizes", "Sizes", "Settings", 5),
        new("Colors", "/settings/colors", "colors", "Colors", "Settings", 6),
        new("POS Billing", "/pos", "POS", "POS Billing", "Operations", 1),
        new("Invoice History", "/sales-invoices", "I", "Sales", "Operations", 2),
        new("Inventory", "/inventory", "Inv", "Inventory", "Operations", 3),
        new("Purchase", "/purchase", "Pu", "Purchase", "Operations", 4),
        new("Stock", "/stock", "St", "Stock", "Operations", 5),
        new("Reports", "/reports", "Rp", "Reports", "Operations", 6),
        new("Orders", "/orders", "O", "Orders", "Operations", 7),
        new("Expenses", "/expenses", "Exp", "Expenses", "Accounts", 1),
        new("Expense Categories", "/expenses/categories", "expensecategories", "Expense Categories", "Accounts", 2),
        new("Cash Dashboard", "/cashflow", "CF", "Cash Flow", "Accounts", 3),
        new("Cash Ledger", "/cashflow/ledger", "CFL", "Cash Flow", "Accounts", 4),
        new("Cash Summary", "/cashflow/summary", "CFS", "Cash Flow", "Accounts", 5),
        new("Receive Payment", "/ledger/customers", "RP", "Party Ledger", "Accounts", 6),
        new("Pay Supplier", "/ledger/suppliers", "PS", "Party Ledger", "Accounts", 7),
        new("Customer Ledger", "/ledger/customers", "CL", "Party Ledger", "Accounts", 8),
        new("Supplier Ledger", "/ledger/suppliers", "SL", "Party Ledger", "Accounts", 9)
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

    private static readonly (string Route, string Icon)[] IconPatches =
    [
        ("/", "D"),
        ("/businesses", "B"),
        ("/branches", "Br"),
        ("/users", "U"),
        ("/roles", "R"),
        ("/menu", "M"),
        ("/categories", "C"),
        ("/subcategories", "SC"),
        ("/brands", "Bn"),
        ("/products", "P"),
        ("/customers", "Cu"),
        ("/suppliers", "Su"),
        ("/units", "Un"),
        ("/settings", "settings"),
        ("/settings/code-sequences", "codeseq"),
        ("/settings/countries", "countries"),
        ("/settings/cities", "cities"),
        ("/settings/sizes", "sizes"),
        ("/settings/colors", "colors"),
        ("/taxes", "T"),
        ("/discounts", "Di"),
        ("/warehouses", "W"),
        ("/pos", "POS"),
        ("/sales-invoices", "I"),
        ("/inventory", "Inv"),
        ("/purchase", "Pu"),
        ("/stock", "St"),
        ("/reports", "Rp"),
        ("/orders", "O"),
        ("/expenses", "Exp"),
        ("/expenses/categories", "expensecategories"),
        ("/cashflow", "CF"),
        ("/cashflow/ledger", "CFL"),
        ("/cashflow/summary", "CFS"),
        ("/ledger/customers", "CL"),
        ("/ledger/suppliers", "SL"),
    ];

    private static Task PatchExistingMenusAsync(POSDbContext context, ILogger logger)
    {
        var patches = new List<Task>
        {
            ExecuteRawSeedAsync(
                context,
                logger,
                "Hide Record Transaction menu",
                """
                UPDATE [Menus] SET [IsActive] = 0
                WHERE [Route] = N'/cashflow/transaction';
                """),
            ExecuteRawSeedAsync(
                context,
                logger,
                "Hide legacy /masters routes",
                """
                UPDATE [Menus] SET [IsActive] = 0
                WHERE [Route] LIKE N'/masters/%'
                   OR [Route] = N'/settings/expense-categories';
                """),
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
                """),
            ExecuteRawSeedAsync(
                context,
                logger,
                "Dashboard ModuleName",
                """
                UPDATE [Menus] SET [ModuleName] = N'Dashboard'
                WHERE [Route] = N'/' AND ([ModuleName] IS NULL OR [ModuleName] = N'POS Billing');
                """)
        };

        foreach (var (route, icon) in IconPatches)
        {
            var sql = $"""
                UPDATE [Menus] SET [Icon] = N'{icon}'
                WHERE [Route] = N'{route}';
                """;
            patches.Add(ExecuteRawSeedAsync(context, logger, $"Icon:{route}", sql));
        }

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
