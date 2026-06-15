using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Common.Constants;

namespace POSSystem.Infrastructure.Data;

public static class PermissionModuleDatabaseInitializer
{
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

        await PermissionModuleSeeder.SeedDefaultModulesAsync(context, logger);
    }
}

public static class PermissionModuleSeeder
{
    private sealed record ModuleSeedEntry(
        string ModuleName,
        string ModuleKey,
        string? ParentKey,
        int DisplayOrder);

    private static readonly ModuleSeedEntry[] DefaultModules =
    [
        new("Catalog", "", null, 1),
        new("Operations", "", null, 2),
        new("Administration", "", null, 3),
        new("Categories", PermissionModules.Categories, "Catalog", 1),
        new("SubCategories", PermissionModules.SubCategories, "Catalog", 2),
        new("Products", PermissionModules.Products, "Catalog", 3),
        new("Menu", PermissionModules.Menu, "Catalog", 4),
        new("Inventory", PermissionModules.Inventory, "Operations", 1),
        new("Orders", PermissionModules.Orders, "Operations", 2),
        new("POS Billing", PermissionModules.PosBilling, "Operations", 3),
        new("Reports", PermissionModules.Reports, "Operations", 4),
        new("Users", PermissionModules.Users, "Administration", 1),
        new("Roles", PermissionModules.Roles, "Administration", 2),
        new("Branches", PermissionModules.Branches, "Administration", 3),
        new("Businesses", PermissionModules.Businesses, "Administration", 4)
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

        await BackfillRolePermissionModuleIdsAsync(context, logger);
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
                .Where(m => m.ModuleName == module.ModuleName && m.ParentModuleId == parentId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();

            if (existing > 0)
                return existing;

            var entity = new Domain.PermissionModule
            {
                ModuleName = module.ModuleName,
                ModuleKey = module.ModuleKey,
                ParentModuleId = parentId,
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
