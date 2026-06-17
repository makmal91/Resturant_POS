using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Common.Constants;

namespace POSSystem.Infrastructure.Data;

public static class PermissionModuleDatabaseInitializer
{
    private const string EnsureModuleKeyIndexSql = """
        IF OBJECT_ID(N'[dbo].[Modules]', N'U') IS NOT NULL
        BEGIN
            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE [name] = N'idx_module_key' AND [object_id] = OBJECT_ID(N'[dbo].[Modules]')
                  AND [has_filter] = 0)
                DROP INDEX [idx_module_key] ON [dbo].[Modules];

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE [name] = N'idx_module_key' AND [object_id] = OBJECT_ID(N'[dbo].[Modules]'))
                CREATE UNIQUE INDEX [idx_module_key] ON [dbo].[Modules]([ModuleKey])
                    WHERE [ModuleKey] <> '' AND [IsDeleted] = 0;
        END
        """;

    private static async Task EnsureModuleKeyIndexAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(EnsureModuleKeyIndexSql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure filtered ModuleKey index.");
        }
    }

    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[Modules]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Modules] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ModuleName] NVARCHAR(100) NOT NULL,
                    [ModuleKey] NVARCHAR(100) NOT NULL DEFAULT '',
                    [ParentModuleId] INT NULL,
                    [Route] NVARCHAR(200) NULL,
                    [Icon] NVARCHAR(50) NULL,
                    [DisplayOrder] INT NOT NULL DEFAULT 0,
                    [IsActive] BIT NOT NULL DEFAULT 1,
                    [IsDeleted] BIT NOT NULL DEFAULT 0,
                    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedDate] DATETIME2 NULL,
                    CONSTRAINT [FK_Modules_ParentModuleId] FOREIGN KEY ([ParentModuleId]) REFERENCES [Modules]([Id])
                );
                CREATE UNIQUE INDEX [idx_module_key] ON [Modules]([ModuleKey]) WHERE [ModuleKey] <> '' AND [IsDeleted] = 0;
            END
            """,
            """
            IF COL_LENGTH('Modules', 'Route') IS NULL
                ALTER TABLE [Modules] ADD [Route] NVARCHAR(200) NULL;
            IF COL_LENGTH('Modules', 'Icon') IS NULL
                ALTER TABLE [Modules] ADD [Icon] NVARCHAR(50) NULL;
            """,
            """
            IF OBJECT_ID(N'[dbo].[ModuleForms]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ModuleForms] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ModuleId] INT NOT NULL,
                    [FormName] NVARCHAR(100) NOT NULL,
                    [FormCode] NVARCHAR(100) NOT NULL,
                    [Route] NVARCHAR(200) NULL,
                    [IsActive] BIT NOT NULL DEFAULT 1,
                    [SortOrder] INT NOT NULL DEFAULT 0,
                    [IsDeleted] BIT NOT NULL DEFAULT 0,
                    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedDate] DATETIME2 NULL,
                    CONSTRAINT [FK_ModuleForms_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [idx_module_form_code] ON [ModuleForms]([FormCode]) WHERE [IsDeleted] = 0;
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[RoleFormPermissions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[RoleFormPermissions] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [RoleId] INT NOT NULL,
                    [FormId] INT NOT NULL,
                    [CanView] BIT NOT NULL DEFAULT 0,
                    [CanCreate] BIT NOT NULL DEFAULT 0,
                    [CanEdit] BIT NOT NULL DEFAULT 0,
                    [CanDelete] BIT NOT NULL DEFAULT 0,
                    [IsDeleted] BIT NOT NULL DEFAULT 0,
                    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedDate] DATETIME2 NULL,
                    CONSTRAINT [FK_RoleFormPermissions_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_RoleFormPermissions_FormId] FOREIGN KEY ([FormId]) REFERENCES [ModuleForms]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [idx_role_form_permission] ON [RoleFormPermissions]([RoleId], [FormId]) WHERE [IsDeleted] = 0;
            END
            """,
            """
            IF COL_LENGTH('RolePermissions', 'ModuleId') IS NULL
                ALTER TABLE [RolePermissions] ADD [ModuleId] INT NULL;
            IF COL_LENGTH('RolePermissions', 'IsDeleted') IS NULL
                ALTER TABLE [RolePermissions] ADD [IsDeleted] BIT NOT NULL CONSTRAINT [DF_RolePermissions_IsDeleted] DEFAULT 0;
            IF COL_LENGTH('RolePermissions', 'CreatedDate') IS NULL
                ALTER TABLE [RolePermissions] ADD [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_RolePermissions_CreatedDate] DEFAULT GETUTCDATE();
            IF COL_LENGTH('RolePermissions', 'UpdatedDate') IS NULL
                ALTER TABLE [RolePermissions] ADD [UpdatedDate] DATETIME2 NULL;
            """
        };

        foreach (var batch in batches)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(batch);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Permission module schema batch skipped or partially applied.");
            }
        }

        await EnsureModuleKeyIndexAsync(context, logger);
        await PermissionModuleSeeder.SeedDefaultModulesAsync(context, logger);
    }
}

public static class PermissionModuleSeeder
{
    private sealed record ModuleSeedEntry(
        string ModuleName,
        string ModuleKey,
        string? ParentKey,
        int DisplayOrder,
        string? Route = null,
        string? Icon = null);

    private static readonly ModuleSeedEntry[] DefaultModules =
    [
        // Top-level standalone
        new("Dashboard", PermissionModules.Dashboard, null, 1, "/", "D"),

        // Parent categories
        new("Business Management", "", null, 2),
        new("User & Role Management", "", null, 3),
        new("Product Management", "", null, 4),
        new("Inventory Management", "", null, 5),
        new("Purchase Management", "", null, 6),
        new("Sales Management", "", null, 7),
        new("Finance", "", null, 8),
        new("Reports", "", null, 9),
        new("Settings", "", null, 10),

        // Business Management
        new("Businesses", PermissionModules.Businesses, "Business Management", 1, "/businesses", "B"),
        new("Branches", PermissionModules.Branches, "Business Management", 2, "/branches", "Br"),

        // User & Role Management
        new("Users", PermissionModules.Users, "User & Role Management", 1, "/users", "U"),
        new("User Roles", PermissionModules.Roles, "User & Role Management", 2, "/roles", "R"),

        // Product Management
        new("Categories", PermissionModules.Categories, "Product Management", 1, "/categories", "C"),
        new("Sub Categories", PermissionModules.SubCategories, "Product Management", 2, "/subcategories", "SC"),
        new("Brands", PermissionModules.Brands, "Product Management", 3, "/brands", "Bn"),
        new("Products", PermissionModules.Products, "Product Management", 4, "/products", "P"),
        new("Units", PermissionModules.Units, "Product Management", 5, "/units", "Un"),
        new("Variants", PermissionModules.Variants, "Product Management", 6, "/products", "V"),

        // Inventory Management
        new("Stock", PermissionModules.Stock, "Inventory Management", 1, "/stock", "St"),
        new("Warehouses", PermissionModules.Warehouses, "Inventory Management", 2, "/warehouses", "W"),
        new("Stock Transfer", PermissionModules.StockTransfer, "Inventory Management", 3, "/inventory", "Inv"),

        // Purchase Management
        new("Purchases", PermissionModules.Purchase, "Purchase Management", 1, "/purchase", "Pu"),
        new("Suppliers", PermissionModules.Suppliers, "Purchase Management", 2, "/suppliers", "Su"),

        // Sales Management
        new("POS", PermissionModules.PosBilling, "Sales Management", 1, "/pos", "POS"),
        new("Sales", PermissionModules.Orders, "Sales Management", 2, "/orders", "S"),
        new("Customers", PermissionModules.Customers, "Sales Management", 3, "/customers", "Cu"),
        new("Invoices", PermissionModules.Sales, "Sales Management", 4, "/sales-invoices", "I"),

        // Finance
        new("Expenses", PermissionModules.Expenses, "Finance", 1, "/expenses", "Exp"),
        new("Expense Categories", PermissionModules.ExpenseCategories, "Finance", 2, "/expenses/categories", "expensecategories"),
        new("Cash Dashboard", PermissionModules.CashFlow, "Finance", 3, "/cashflow", "CF"),
        new("Cash Ledger", "CashFlow.Ledger", "Finance", 4, "/cashflow/ledger", "CFL"),
        new("Cash Summary", "CashFlow.Summary", "Finance", 5, "/cashflow/summary", "CFS"),

        // Reports
        new("Sales Reports", PermissionModules.SalesReports, "Reports", 1, "/reports", "Rp"),
        new("Purchase Reports", PermissionModules.PurchaseReports, "Reports", 2, "/reports", "Rp"),
        new("Stock Reports", PermissionModules.StockReports, "Reports", 3, "/reports", "Rp"),

        // Settings
        new("System Settings", PermissionModules.SystemSettings, "Settings", 1, "/settings", "settings"),
        new("Code Sequences", PermissionModules.CodeSequences, "Settings", 2, "/settings/code-sequences", "codeseq"),
        new("Countries", PermissionModules.Countries, "Settings", 3, "/settings/countries", "countries"),
        new("Cities", PermissionModules.Cities, "Settings", 4, "/settings/cities", "cities"),
        new("Sizes", PermissionModules.Sizes, "Settings", 5, "/settings/sizes", "sizes"),
        new("Colors", PermissionModules.Colors, "Settings", 6, "/settings/colors", "colors"),
    ];

    public static async Task SeedDefaultModulesAsync(POSDbContext context, ILogger logger)
    {
        var keyToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in DefaultModules.Where(m => m.ParentKey == null))
        {
            var id = await EnsureModuleAsync(context, logger, group, null);
            if (id > 0)
                keyToId[group.ModuleName] = id;
        }

        foreach (var module in DefaultModules.Where(m => m.ParentKey != null))
        {
            if (!keyToId.TryGetValue(module.ParentKey!, out var parentId))
                continue;

            await EnsureModuleAsync(context, logger, module, parentId);
        }

        await DeactivateLegacyModulesAsync(context, logger);
        await DeactivateEmptyMasterDataGroupAsync(context, logger);
        await BackfillRolePermissionModuleIdsAsync(context, logger);
        await ModuleFormSeeder.SeedDefaultFormsAsync(context, logger);
    }

    private static async Task DeactivateLegacyModulesAsync(POSDbContext context, ILogger logger)
    {
        var legacyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Catalog", "Operations", "Accounts", "Administration",
            "Master Data", "Menu", "Inventory", "Purchase", "POS Billing",
            "Reports", "Orders", "Taxes", "Discounts",
            "Record Transaction"
        };

        try
        {
            var legacyModules = await context.PermissionModules
                .IgnoreQueryFilters()
                .Where(m => legacyNames.Contains(m.ModuleName) && m.IsActive)
                .ToListAsync();

            foreach (var module in legacyModules)
            {
                module.IsActive = false;
                module.UpdatedDate = DateTime.UtcNow;
            }

            if (legacyModules.Count > 0)
                await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deactivate legacy modules.");
        }
    }

    private static async Task DeactivateEmptyMasterDataGroupAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync("""
                UPDATE [Modules] SET [IsActive] = 0, [UpdatedDate] = GETUTCDATE()
                WHERE [ModuleName] = N'Master Data' AND ([ModuleKey] = N'' OR [ModuleKey] IS NULL);
                UPDATE [Menus] SET [IsActive] = 0
                WHERE [Route] LIKE N'/masters/%';
                """);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deactivate legacy Master Data navigation.");
        }
    }

    private static async Task<int> EnsureModuleAsync(
        POSDbContext context,
        ILogger logger,
        ModuleSeedEntry module,
        int? parentId)
    {
        try
        {
            var existing = await context.PermissionModules
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m =>
                    (module.ModuleKey != string.Empty && m.ModuleKey == module.ModuleKey) ||
                    (module.ModuleKey == string.Empty && m.ModuleName == module.ModuleName && m.ParentModuleId == parentId));

            if (existing != null)
            {
                existing.ModuleName = module.ModuleName;
                existing.ParentModuleId = parentId;
                existing.DisplayOrder = module.DisplayOrder;
                existing.Route = module.Route;
                existing.Icon = module.Icon;
                existing.IsActive = true;
                existing.IsDeleted = false;
                existing.UpdatedDate = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return existing.Id;
            }

            var entity = new Domain.PermissionModule
            {
                ModuleName = module.ModuleName,
                ModuleKey = module.ModuleKey,
                ParentModuleId = parentId,
                Route = module.Route,
                Icon = module.Icon,
                DisplayOrder = module.DisplayOrder,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

            await context.PermissionModules.AddAsync(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }
        catch (Exception ex)
        {
            foreach (var entry in context.ChangeTracker.Entries<Domain.PermissionModule>()
                         .Where(e => e.State == EntityState.Added))
            {
                entry.State = EntityState.Detached;
            }

            logger.LogWarning(ex, "Failed to seed module {ModuleName}", module.ModuleName);
            return 0;
        }
    }

    private static async Task BackfillRolePermissionModuleIdsAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var modules = await context.PermissionModules
                .AsNoTracking()
                .Where(m => m.ModuleKey != string.Empty)
                .Select(m => new { m.Id, m.ModuleKey, m.ModuleName })
                .ToListAsync();

            var moduleMap = modules.ToDictionary(
                m => m.ModuleKey,
                m => m.Id,
                StringComparer.OrdinalIgnoreCase);

            var permissions = await context.RolePermissions
                .IgnoreQueryFilters()
                .Where(rp => rp.ModuleId == null)
                .ToListAsync();

            foreach (var permission in permissions)
            {
                if (moduleMap.TryGetValue(permission.ModuleName, out var moduleId))
                    permission.ModuleId = moduleId;
            }

            if (permissions.Count > 0)
                await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to backfill RolePermission ModuleId values.");
        }
    }
}

public static class ModuleFormSeeder
{
    public static async Task SeedDefaultFormsAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var modules = await context.PermissionModules
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.IsActive && m.ModuleKey != string.Empty)
                .Select(m => new { m.Id, m.ModuleKey, m.ModuleName, m.Route })
                .ToListAsync();

            foreach (var module in modules)
            {
                var formCode = $"{module.ModuleKey}_Main";
                var exists = await context.ModuleForms
                    .IgnoreQueryFilters()
                    .AnyAsync(f => f.FormCode == formCode);

                if (exists)
                    continue;

                await context.ModuleForms.AddAsync(new Domain.ModuleForm
                {
                    ModuleId = module.Id,
                    FormName = $"{module.ModuleName} Form",
                    FormCode = formCode,
                    Route = module.Route,
                    SortOrder = 1,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            await BackfillRoleFormPermissionsAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed module forms.");
        }
    }

    private static async Task BackfillRoleFormPermissionsAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var roles = await context.Roles
                .AsNoTracking()
                .Where(r => !r.IsDeleted)
                .Select(r => r.Id)
                .ToListAsync();

            var forms = await context.ModuleForms
                .AsNoTracking()
                .Include(f => f.Module)
                .Where(f => !f.IsDeleted && f.IsActive)
                .ToListAsync();

            foreach (var roleId in roles)
            {
                var modulePermissions = await context.RolePermissions
                    .AsNoTracking()
                    .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
                    .ToListAsync();

                foreach (var form in forms)
                {
                    var exists = await context.RoleFormPermissions
                        .IgnoreQueryFilters()
                        .AnyAsync(rfp => rfp.RoleId == roleId && rfp.FormId == form.Id);

                    if (exists)
                        continue;

                    var modulePerm = modulePermissions.FirstOrDefault(p =>
                        string.Equals(p.ModuleName, form.Module.ModuleName, StringComparison.OrdinalIgnoreCase) ||
                        (form.Module.ModuleKey != string.Empty &&
                         string.Equals(p.ModuleName, form.Module.ModuleKey, StringComparison.OrdinalIgnoreCase)));

                    await context.RoleFormPermissions.AddAsync(new Domain.RoleFormPermission
                    {
                        RoleId = roleId,
                        FormId = form.Id,
                        CanView = modulePerm?.CanView ?? false,
                        CanCreate = modulePerm?.CanCreate ?? false,
                        CanEdit = modulePerm?.CanEdit ?? false,
                        CanDelete = modulePerm?.CanDelete ?? false,
                        IsDeleted = false,
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to backfill role form permissions.");
        }
    }
}
