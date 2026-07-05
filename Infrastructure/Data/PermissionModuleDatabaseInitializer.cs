using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Common.Constants;
using POSSystem.Domain;

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
        await ApplyFinanceActionRoutePatchesAsync(context, logger);
    }

    /// <summary>
    /// Runs on every schema patch startup (not only full seed) so dev/prod DBs pick up finance screen routes.
    /// </summary>
    internal static async Task ApplyFinanceActionRoutePatchesAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync("""
                UPDATE [Modules] SET
                    [Route] = N'/finance/receivables',
                    [ModuleName] = N'Receivables',
                    [Icon] = N'RCV',
                    [IsActive] = 1,
                    [UpdatedDate] = GETUTCDATE()
                WHERE [ModuleKey] = N'Party Ledger';

                UPDATE [Modules] SET
                    [Route] = N'/finance/payables',
                    [ModuleName] = N'Payables',
                    [Icon] = N'PAY',
                    [IsActive] = 1,
                    [UpdatedDate] = GETUTCDATE()
                WHERE [ModuleKey] = N'PartyLedger.PaySupplier';

                UPDATE [Modules] SET
                    [Route] = N'/finance/expenses',
                    [UpdatedDate] = GETUTCDATE()
                WHERE [ModuleKey] = N'Expenses' AND [Route] = N'/expenses';

                IF NOT EXISTS (SELECT 1 FROM [Modules] WHERE [ModuleKey] = N'CashFlow.JournalVoucher')
                BEGIN
                    INSERT INTO [Modules] ([ModuleName], [ModuleKey], [ParentModuleId], [DisplayOrder], [Route], [Icon], [IsActive], [CreatedDate], [IsDeleted])
                    SELECT N'Journal Vouchers', N'CashFlow.JournalVoucher', p.[Id], 6, N'/finance/journal-vouchers', N'JV', 1, GETUTCDATE(), 0
                    FROM [Modules] p
                    WHERE p.[ModuleName] = N'Finance' AND p.[ParentModuleId] IS NULL;
                END

                UPDATE [Modules] SET
                    [Route] = N'/finance/journal-vouchers',
                    [ModuleName] = N'Journal Vouchers',
                    [Icon] = N'JV',
                    [IsActive] = 1,
                    [UpdatedDate] = GETUTCDATE()
                WHERE [ModuleKey] = N'CashFlow.JournalVoucher';

                IF NOT EXISTS (SELECT 1 FROM [Modules] WHERE [ModuleKey] = N'Account Ledger')
                BEGIN
                    INSERT INTO [Modules] ([ModuleName], [ModuleKey], [ParentModuleId], [DisplayOrder], [Route], [Icon], [IsActive], [CreatedDate], [IsDeleted])
                    SELECT N'Account Ledger', N'Account Ledger', p.[Id], 11, N'/accounting/ledger', N'AL', 1, GETUTCDATE(), 0
                    FROM [Modules] p
                    WHERE p.[ModuleName] = N'Finance' AND p.[ParentModuleId] IS NULL;
                END

                UPDATE [Modules] SET
                    [Route] = N'/accounting/ledger',
                    [ModuleName] = N'Account Ledger',
                    [Icon] = N'AL',
                    [IsActive] = 1,
                    [UpdatedDate] = GETUTCDATE()
                WHERE [ModuleKey] = N'Account Ledger';

                INSERT INTO [RolePermissions] ([RoleId], [ModuleId], [ModuleName], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanExport], [CanUpload], [IsDeleted], [CreatedDate])
                SELECT r.[Id], m.[Id], N'Account Ledger', 1, 1, 1, 1, 1, 1, 0, GETUTCDATE()
                FROM [Roles] r
                CROSS JOIN [Modules] m
                WHERE m.[ModuleKey] = N'Account Ledger' AND m.[IsDeleted] = 0
                  AND r.[Name] IN (N'Admin', N'System Admin', N'Manager')
                  AND r.[IsDeleted] = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM [RolePermissions] rp
                      WHERE rp.[RoleId] = r.[Id]
                        AND rp.[ModuleName] = N'Account Ledger'
                        AND rp.[IsDeleted] = 0);

                UPDATE [ModuleForms] SET [FormName] = N'Journal Voucher'
                WHERE [FormCode] = N'CashFlow_RecordTransaction';

                IF NOT EXISTS (SELECT 1 FROM [Modules] WHERE [ModuleKey] = N'Trial Balance Report')
                BEGIN
                    INSERT INTO [Modules] ([ModuleName], [ModuleKey], [ParentModuleId], [DisplayOrder], [Route], [Icon], [IsActive], [CreatedDate], [IsDeleted])
                    SELECT N'Trial Balance', N'Trial Balance Report', p.[Id], 11, N'/reports/trial-balance', N'TB', 1, GETUTCDATE(), 0
                    FROM [Modules] p
                    WHERE p.[ModuleName] = N'Reports' AND p.[ParentModuleId] IS NULL;
                END

                UPDATE [Modules] SET
                    [Route] = N'/reports/trial-balance',
                    [ModuleName] = N'Trial Balance',
                    [Icon] = N'TB',
                    [IsActive] = 1,
                    [UpdatedDate] = GETUTCDATE()
                WHERE [ModuleKey] = N'Trial Balance Report';

                INSERT INTO [RolePermissions] ([RoleId], [ModuleId], [ModuleName], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanExport], [CanUpload], [IsDeleted], [CreatedDate])
                SELECT r.[Id], m.[Id], N'Trial Balance Report', 1, 1, 1, 1, 1, 1, 0, GETUTCDATE()
                FROM [Roles] r
                CROSS JOIN [Modules] m
                WHERE m.[ModuleKey] = N'Trial Balance Report' AND m.[IsDeleted] = 0
                  AND r.[Name] IN (N'Admin', N'System Admin', N'Manager')
                  AND r.[IsDeleted] = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM [RolePermissions] rp
                      WHERE rp.[RoleId] = r.[Id]
                        AND rp.[ModuleName] = N'Trial Balance Report'
                        AND rp.[IsDeleted] = 0);
                """);

            await EnsureTrialBalanceReportModuleAsync(context, logger);
            await EnsureRegisterHistoryModuleAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to apply finance action route patches.");
        }
    }

    /// <summary>
    /// Ensures Trial Balance exists under Reports in Modules (sidebar uses Modules, not Menus).
    /// </summary>
    internal static async Task EnsureTrialBalanceReportModuleAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var reportsParent = await context.PermissionModules
                .IgnoreQueryFilters()
                .Where(m => !m.IsDeleted
                            && m.ParentModuleId == null
                            && m.ModuleName == "Reports")
                .OrderBy(m => string.IsNullOrEmpty(m.ModuleKey) ? 0 : 1)
                .ThenBy(m => m.Id)
                .FirstOrDefaultAsync();

            if (reportsParent == null)
            {
                logger.LogWarning("Trial Balance sidebar patch skipped: Reports parent module not found.");
                return;
            }

            var module = await context.PermissionModules
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.ModuleKey == PermissionModules.TrialBalanceReport);

            if (module == null)
            {
                module = new PermissionModule
                {
                    ModuleName = "Trial Balance",
                    ModuleKey = PermissionModules.TrialBalanceReport,
                    ParentModuleId = reportsParent.Id,
                    Route = "/reports/trial-balance",
                    Icon = "TB",
                    DisplayOrder = 11,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow,
                };
                await context.PermissionModules.AddAsync(module);
            }
            else
            {
                module.ModuleName = "Trial Balance";
                module.ParentModuleId = reportsParent.Id;
                module.Route = "/reports/trial-balance";
                module.Icon = "TB";
                module.DisplayOrder = 11;
                module.IsActive = true;
                module.IsDeleted = false;
                module.UpdatedDate = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();

            var privilegedRoleNames = new[] { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Manager };
            var privilegedRoleIds = await context.Roles
                .AsNoTracking()
                .Where(r => !r.IsDeleted && privilegedRoleNames.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var rolesWithProfitLoss = await context.RolePermissions
                .AsNoTracking()
                .Where(rp => !rp.IsDeleted && rp.CanView && rp.ModuleName == PermissionModules.ProfitLossReport)
                .Select(rp => rp.RoleId)
                .Distinct()
                .ToListAsync();

            var roleIdsToGrant = privilegedRoleIds
                .Union(rolesWithProfitLoss)
                .Distinct()
                .ToList();

            foreach (var roleId in roleIdsToGrant)
            {
                var permission = await context.RolePermissions
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(rp =>
                        rp.RoleId == roleId
                        && (rp.ModuleId == module.Id || rp.ModuleName == PermissionModules.TrialBalanceReport));

                if (permission == null)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = roleId,
                        ModuleId = module.Id,
                        ModuleName = PermissionModules.TrialBalanceReport,
                        CanView = true,
                        CanCreate = false,
                        CanEdit = false,
                        CanDelete = false,
                        CanExport = true,
                        CanUpload = false,
                        IsDeleted = false,
                        CreatedDate = DateTime.UtcNow,
                    });
                    continue;
                }

                permission.ModuleId = module.Id;
                permission.ModuleName = PermissionModules.TrialBalanceReport;
                permission.CanView = true;
                permission.CanExport = true;
                permission.IsDeleted = false;
                permission.UpdatedDate = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure Trial Balance report module for sidebar.");
        }
    }

    /// <summary>Ensures Register History appears under Finance (sidebar uses Modules).</summary>
    internal static async Task EnsureRegisterHistoryModuleAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var financeParent = await context.PermissionModules
                .IgnoreQueryFilters()
                .Where(m => !m.IsDeleted && m.ParentModuleId == null && m.ModuleName == "Finance")
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync();

            if (financeParent == null)
            {
                logger.LogWarning("Register History sidebar patch skipped: Finance parent module not found.");
                return;
            }

            var module = await context.PermissionModules
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.ModuleKey == PermissionModules.RegisterHistoryReport);

            if (module == null)
            {
                module = new PermissionModule
                {
                    ModuleName = "Register History",
                    ModuleKey = PermissionModules.RegisterHistoryReport,
                    ParentModuleId = financeParent.Id,
                    Route = "/cashflow/register-history",
                    Icon = "RH",
                    DisplayOrder = 7,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow,
                };
                await context.PermissionModules.AddAsync(module);
            }
            else
            {
                module.ModuleName = "Register History";
                module.ParentModuleId = financeParent.Id;
                module.Route = "/cashflow/register-history";
                module.Icon = "RH";
                module.DisplayOrder = 7;
                module.IsActive = true;
                module.IsDeleted = false;
                module.UpdatedDate = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();

            var privilegedRoleNames = new[] { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Manager };
            var privilegedRoleIds = await context.Roles
                .AsNoTracking()
                .Where(r => !r.IsDeleted && privilegedRoleNames.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var rolesWithCashFlow = await context.RolePermissions
                .AsNoTracking()
                .Where(rp => !rp.IsDeleted && rp.CanView && rp.ModuleName == PermissionModules.CashFlow)
                .Select(rp => rp.RoleId)
                .Distinct()
                .ToListAsync();

            foreach (var roleId in privilegedRoleIds.Union(rolesWithCashFlow).Distinct())
            {
                var permission = await context.RolePermissions
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(rp =>
                        rp.RoleId == roleId
                        && (rp.ModuleId == module.Id || rp.ModuleName == PermissionModules.RegisterHistoryReport));

                if (permission == null)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = roleId,
                        ModuleId = module.Id,
                        ModuleName = PermissionModules.RegisterHistoryReport,
                        CanView = true,
                        CanCreate = false,
                        CanEdit = false,
                        CanDelete = false,
                        CanExport = true,
                        CanUpload = false,
                        IsDeleted = false,
                        CreatedDate = DateTime.UtcNow,
                    });
                    continue;
                }

                permission.ModuleId = module.Id;
                permission.ModuleName = PermissionModules.RegisterHistoryReport;
                permission.CanView = true;
                permission.CanExport = true;
                permission.IsDeleted = false;
                permission.UpdatedDate = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure Register History module for sidebar.");
        }
    }
}

public static class PermissionModuleSeeder
{
    public static int ExpectedModuleCount => DefaultModules.Length;

    public static IReadOnlyList<string> ExpectedModuleKeys =>
        DefaultModules
            .Where(m => !string.IsNullOrWhiteSpace(m.ModuleKey))
            .Select(m => m.ModuleKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

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
        new("Variants", PermissionModules.Variants, "Product Management", 6, null, "V"),
        new("Barcodes", PermissionModules.Barcodes, "Product Management", 7, "/barcodes", "Bc"),

        // Inventory Management
        new("Stock", PermissionModules.Stock, "Inventory Management", 1, "/stock", "St"),
        new("Warehouses", PermissionModules.Warehouses, "Inventory Management", 2, "/warehouses", "W"),
        new("Stock Transfer", PermissionModules.StockTransfer, "Inventory Management", 3, "/inventory", "Inv"),
        new("Opening Stock", PermissionModules.OpeningStock, "Inventory Management", 4, "/opening-stock", "OS"),

        // Purchase Management
        new("Purchases", PermissionModules.Purchase, "Purchase Management", 1, "/purchase", "Pu"),
        new("Suppliers", PermissionModules.Suppliers, "Purchase Management", 2, "/suppliers", "Su"),

        // Sales Management
        new("POS", PermissionModules.PosBilling, "Sales Management", 1, "/pos", "POS"),
        new("Sales", PermissionModules.Orders, "Sales Management", 2, "/orders", "S"),
        new("Customers", PermissionModules.Customers, "Sales Management", 3, "/customers", "Cu"),
        new("Invoices", PermissionModules.Sales, "Sales Management", 4, "/sales-invoices", "I"),

        // Finance
        new("Expenses", PermissionModules.Expenses, "Finance", 1, "/finance/expenses", "Exp"),
        new("Expense Categories", PermissionModules.ExpenseCategories, "Finance", 2, "/expenses/categories", "expensecategories"),
        new("Cash Dashboard", PermissionModules.CashFlow, "Finance", 3, "/cashflow", "CF"),
        new("Cash Ledger", "CashFlow.Ledger", "Finance", 4, "/cashflow/ledger", "CFL"),
        new("Cash Summary", "CashFlow.Summary", "Finance", 5, "/cashflow/summary", "CFS"),
        new("Journal Vouchers", "CashFlow.JournalVoucher", "Finance", 6, "/finance/journal-vouchers", "JV"),

        // Party Ledger — action screens (payments) vs read-only ledgers
        new("Receivables", PermissionModules.PartyLedger, "Finance", 7, "/finance/receivables", "RCV"),
        new("Payables", "PartyLedger.PaySupplier", "Finance", 8, "/finance/payables", "PAY"),
        new("Customer Ledger", "PartyLedger.CustomerLedger", "Finance", 9, "/ledger/customers", "CL"),
        new("Supplier Ledger", "PartyLedger.SupplierLedger", "Finance", 10, "/ledger/suppliers", "SL"),
        new("Account Ledger", PermissionModules.AccountLedger, "Finance", 11, "/accounting/ledger", "AL"),

        // Reports
        new("Sales Report", PermissionModules.SalesReports, "Reports", 1, "/reports/sales", "SR"),
        new("Product Wise Sales", PermissionModules.ProductWiseSalesReport, "Reports", 2, "/reports/product-wise-sales", "PWS"),
        new("Purchase Report", PermissionModules.PurchaseReports, "Reports", 3, "/reports/purchases", "PR"),
        new("Customer Outstanding", PermissionModules.CustomerOutstandingReport, "Reports", 4, "/reports/customer-outstanding", "CO"),
        new("Supplier Payable", PermissionModules.SupplierPayableReport, "Reports", 5, "/reports/supplier-payable", "SP"),
        new("Profit & Loss", PermissionModules.ProfitLossReport, "Reports", 6, "/reports/profit-loss", "PL"),
        new("Trial Balance", PermissionModules.TrialBalanceReport, "Reports", 11, "/reports/trial-balance", "TB"),
        new("Stock Report", PermissionModules.StockReports, "Reports", 7, "/reports/stock", "StR"),
        new("Stock By Unit Report", "StockReports.ByUnit", "Reports", 10, "/reports/stock-by-unit", "SBU"),
        new("Receivable Aging", PermissionModules.CustomerReceivableAgingReport, "Reports", 8, "/reports/receivable-aging", "RA"),
        new("Payable Aging", PermissionModules.SupplierPayableAgingReport, "Reports", 9, "/reports/payable-aging", "PA"),

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
        await PermissionModuleDatabaseInitializer.EnsureSchemaAsync(context, logger);

        foreach (var group in DefaultModules.Where(m => m.ParentKey == null))
            await EnsureModuleAsync(context, logger, group, null);

        await ConsolidateDuplicateTopLevelModulesAsync(context, logger);

        var keyToId = await LoadTopLevelModuleIdsAsync(context);

        var skipped = 0;
        foreach (var module in DefaultModules.Where(m => m.ParentKey != null))
        {
            if (!keyToId.TryGetValue(module.ParentKey!, out var parentId))
            {
                logger.LogError(
                    "Module seed skipped {ModuleName}: parent group '{ParentKey}' not found in database.",
                    module.ModuleName,
                    module.ParentKey);
                skipped++;
                continue;
            }

            await EnsureModuleAsync(context, logger, module, parentId);
        }

        if (skipped > 0)
        {
            logger.LogWarning(
                "Module seed completed with {Skipped} skipped child module(s). Re-run seed after fixing parent groups.",
                skipped);
        }

        await DeactivateLegacyModulesAsync(context, logger);
        await DeactivateOldReportSubMenuItemsAsync(context, logger);
        await MigrateFinanceActionModuleRoutesAsync(context, logger);
        await DeactivateEmptyMasterDataGroupAsync(context, logger);
        await BackfillRolePermissionModuleIdsAsync(context, logger);
        await ModuleFormSeeder.SeedDefaultFormsAsync(context, logger);

        var activeCount = await context.PermissionModules
            .IgnoreQueryFilters()
            .CountAsync(m => !m.IsDeleted && m.IsActive);
        logger.LogInformation(
            "Module seed finished: {ActiveCount} active modules (expected {ExpectedCount}).",
            activeCount,
            DefaultModules.Length);
    }

    private static async Task<Dictionary<string, int>> LoadTopLevelModuleIdsAsync(POSDbContext context)
    {
        var topLevelNames = DefaultModules
            .Where(m => m.ParentKey == null)
            .Select(m => m.ModuleName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = await context.PermissionModules
            .IgnoreQueryFilters()
            .Where(m => !m.IsDeleted && m.ParentModuleId == null && topLevelNames.Contains(m.ModuleName))
            .Select(m => new { m.ModuleName, m.Id, m.ModuleKey })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ModuleName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(r => string.IsNullOrEmpty(r.ModuleKey) ? 0 : 1)
                    .ThenBy(r => r.Id)
                    .First()
                    .Id,
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Legacy seeds may have created duplicate top-level groups (e.g. Reports parent + old Reports module).
    /// Keeps the empty ModuleKey parent row and soft-deletes duplicates, reparenting children.
    /// </summary>
    private static async Task ConsolidateDuplicateTopLevelModulesAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var topLevelNames = DefaultModules
                .Where(m => m.ParentKey == null)
                .Select(m => m.ModuleName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var topLevels = await context.PermissionModules
                .IgnoreQueryFilters()
                .Where(m => !m.IsDeleted && m.ParentModuleId == null && topLevelNames.Contains(m.ModuleName))
                .OrderBy(m => m.Id)
                .ToListAsync();

            var changed = false;
            foreach (var group in topLevels.GroupBy(m => m.ModuleName, StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() <= 1)
                    continue;

                var keep = group
                    .OrderBy(m => string.IsNullOrEmpty(m.ModuleKey) ? 0 : 1)
                    .ThenBy(m => m.Id)
                    .First();

                foreach (var duplicate in group.Where(m => m.Id != keep.Id))
                {
                    var children = await context.PermissionModules
                        .IgnoreQueryFilters()
                        .Where(m => m.ParentModuleId == duplicate.Id && !m.IsDeleted)
                        .ToListAsync();

                    foreach (var child in children)
                        child.ParentModuleId = keep.Id;

                    duplicate.IsDeleted = true;
                    duplicate.IsActive = false;
                    duplicate.UpdatedDate = DateTime.UtcNow;
                    changed = true;
                }

                logger.LogWarning(
                    "Consolidated duplicate top-level module '{ModuleName}'; kept Id {KeepId}.",
                    group.Key,
                    keep.Id);
            }

            if (changed)
                await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to consolidate duplicate top-level modules.");
        }
    }

    private static async Task DeactivateLegacyModulesAsync(POSDbContext context, ILogger logger)
    {
        var legacyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Catalog", "Operations", "Accounts", "Administration",
            "Master Data", "Menu", "Inventory", "Purchase", "POS Billing",
            "Orders", "Taxes", "Discounts",
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

    /// <summary>
    /// Deactivate legacy standalone Reports menu entry (replaced by per-report modules).
    /// </summary>
    private static async Task DeactivateOldReportSubMenuItemsAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var modules = await context.PermissionModules
                .IgnoreQueryFilters()
                .Where(m => m.ModuleKey == PermissionModules.Reports
                         && m.ParentModuleId == null
                         && m.IsActive)
                .ToListAsync();

            foreach (var module in modules)
            {
                module.IsActive = false;
                module.UpdatedDate = DateTime.UtcNow;
            }

            if (modules.Count > 0)
                await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deactivate legacy standalone Reports module.");
        }
    }

    private static async Task MigrateFinanceActionModuleRoutesAsync(POSDbContext context, ILogger logger)
    {
        await PermissionModuleDatabaseInitializer.ApplyFinanceActionRoutePatchesAsync(context, logger);
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
                    (module.ModuleKey == string.Empty && m.ModuleName == module.ModuleName && m.ParentModuleId == parentId) ||
                    (!string.IsNullOrEmpty(module.Route) && m.Route == module.Route));

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

            logger.LogError(ex, "Failed to seed module {ModuleName} (key: {ModuleKey}).", module.ModuleName, module.ModuleKey);
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

            await SeedAdditionalFormsAsync(context);

            await context.SaveChangesAsync();
            await BackfillRoleFormPermissionsAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed module forms.");
        }
    }

    private static readonly (string ModuleKey, string FormCode, string FormName, int SortOrder)[] AdditionalForms =
    [
        ("Cash Flow", "CashFlow_JournalVoucher", "Journal Voucher", 2),
        ("Party Ledger", "PartyLedger_ReceivePayment", "Receive Payment", 2),
        ("PartyLedger.PaySupplier", "PartyLedger_PaySupplier", "Pay Supplier", 2),
    ];

    private static async Task SeedAdditionalFormsAsync(POSDbContext context)
    {
        var moduleIds = await context.PermissionModules
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.IsActive && m.ModuleKey != string.Empty)
            .Select(m => new { m.Id, m.ModuleKey })
            .ToListAsync();

        var moduleByKey = moduleIds.ToDictionary(m => m.ModuleKey, m => m.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var (moduleKey, formCode, formName, sortOrder) in AdditionalForms)
        {
            if (!moduleByKey.TryGetValue(moduleKey, out var moduleId))
                continue;

            var exists = await context.ModuleForms
                .IgnoreQueryFilters()
                .AnyAsync(f => f.FormCode == formCode);

            if (exists)
                continue;

            await context.ModuleForms.AddAsync(new Domain.ModuleForm
            {
                ModuleId = moduleId,
                FormName = formName,
                FormCode = formCode,
                Route = null,
                SortOrder = sortOrder,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            });
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
