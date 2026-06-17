using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class VariantMasterDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[Branches]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[Sizes]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Sizes] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(50) NOT NULL,
                    [SortOrder] INT NOT NULL CONSTRAINT [DF_Sizes_SortOrder] DEFAULT 0,
                    [IsActive] BIT NOT NULL CONSTRAINT [DF_Sizes_IsActive] DEFAULT 1,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_Sizes_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Sizes_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Sizes_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_Sizes_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[Branches]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[Colors]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Colors] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(50) NOT NULL,
                    [HexCode] NVARCHAR(7) NULL,
                    [IsActive] BIT NOT NULL CONSTRAINT [DF_Colors_IsActive] DEFAULT 1,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_Colors_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Colors_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Colors_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_Colors_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[Sizes]', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_size_business_branch_name' AND object_id = OBJECT_ID(N'[dbo].[Sizes]'))
                CREATE UNIQUE INDEX [idx_size_business_branch_name] ON [dbo].[Sizes]([BusinessId], [BranchId], [Name]) WHERE [IsDeleted] = 0;
            IF OBJECT_ID(N'[dbo].[Colors]', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_color_business_branch_name' AND object_id = OBJECT_ID(N'[dbo].[Colors]'))
                CREATE UNIQUE INDEX [idx_color_business_branch_name] ON [dbo].[Colors]([BusinessId], [BranchId], [Name]) WHERE [IsDeleted] = 0;
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
                logger.LogWarning(ex, "Variant master schema batch skipped or partially applied.");
            }
        }

        await SeedDefaultSizesAndColorsAsync(context, logger);
    }

    private static async Task SeedDefaultSizesAndColorsAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            if (!await context.Database.CanConnectAsync())
                return;

            var branches = await context.Branches
                .IgnoreQueryFilters()
                .Where(b => !b.IsDeleted)
                .Select(b => new { b.Id, b.BusinessId })
                .ToListAsync();

            if (branches.Count == 0)
                return;

            var sizes = new (string Name, int SortOrder)[]
            {
                ("S", 1),
                ("M", 2),
                ("L", 3),
                ("XL", 4),
                ("XXL", 5),
            };

            var colors = new (string Name, string? HexCode)[]
            {
                ("Black", "#000000"),
                ("White", "#FFFFFF"),
                ("Blue", "#0000FF"),
                ("Red", "#FF0000"),
                ("Green", "#008000"),
            };

            foreach (var branch in branches)
            {
                foreach (var size in sizes)
                {
                    try
                    {
                        await context.Database.ExecuteSqlInterpolatedAsync($"""
                            IF NOT EXISTS (
                                SELECT 1 FROM [Sizes]
                                WHERE [BusinessId] = {branch.BusinessId}
                                  AND [BranchId] = {branch.Id}
                                  AND [Name] = {size.Name}
                                  AND [IsDeleted] = 0)
                            BEGIN
                                INSERT INTO [Sizes] ([Name], [SortOrder], [IsActive], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                                VALUES ({size.Name}, {size.SortOrder}, 1, {branch.BusinessId}, {branch.Id}, GETUTCDATE(), 0);
                            END
                            """);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to seed size {SizeName} for branch {BranchId}.", size.Name, branch.Id);
                    }
                }

                foreach (var color in colors)
                {
                    try
                    {
                        await context.Database.ExecuteSqlInterpolatedAsync($"""
                            IF NOT EXISTS (
                                SELECT 1 FROM [Colors]
                                WHERE [BusinessId] = {branch.BusinessId}
                                  AND [BranchId] = {branch.Id}
                                  AND [Name] = {color.Name}
                                  AND [IsDeleted] = 0)
                            BEGIN
                                INSERT INTO [Colors] ([Name], [HexCode], [IsActive], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                                VALUES ({color.Name}, {color.HexCode}, 1, {branch.BusinessId}, {branch.Id}, GETUTCDATE(), 0);
                            END
                            """);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to seed color {ColorName} for branch {BranchId}.", color.Name, branch.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Default size/color seed skipped.");
        }
    }
}
