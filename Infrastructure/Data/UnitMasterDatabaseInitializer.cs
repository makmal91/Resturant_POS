using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class UnitMasterDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[Units]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Units] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(100) NOT NULL,
                    [Code] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Units_Code] DEFAULT N'',
                    [Description] NVARCHAR(500) NOT NULL CONSTRAINT [DF_Units_Description] DEFAULT N'',
                    [ConversionFactor] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_Units_ConversionFactor] DEFAULT 1,
                    [Status] BIT NOT NULL CONSTRAINT [DF_Units_Status] DEFAULT 1,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_Units_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Units_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Units_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_Units_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            """
            IF COL_LENGTH('Units', 'Code') IS NULL
                ALTER TABLE [Units] ADD [Code] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Units_Code] DEFAULT N'';
            IF COL_LENGTH('Units', 'Description') IS NULL
                ALTER TABLE [Units] ADD [Description] NVARCHAR(500) NOT NULL CONSTRAINT [DF_Units_Description] DEFAULT N'';
            IF COL_LENGTH('Units', 'ConversionFactor') IS NULL
                ALTER TABLE [Units] ADD [ConversionFactor] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_Units_ConversionFactor] DEFAULT 1;
            IF COL_LENGTH('Units', 'Status') IS NULL
                ALTER TABLE [Units] ADD [Status] BIT NOT NULL CONSTRAINT [DF_Units_Status] DEFAULT 1;
            IF COL_LENGTH('Units', 'BusinessId') IS NULL
                ALTER TABLE [Units] ADD [BusinessId] INT NOT NULL CONSTRAINT [DF_Units_BusinessId] DEFAULT 1;
            IF COL_LENGTH('Units', 'BranchId') IS NULL
                ALTER TABLE [Units] ADD [BranchId] INT NOT NULL CONSTRAINT [DF_Units_BranchId] DEFAULT 1;
            IF COL_LENGTH('Units', 'CreatedDate') IS NULL
                ALTER TABLE [Units] ADD [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Units_CreatedDate] DEFAULT GETUTCDATE();
            IF COL_LENGTH('Units', 'IsDeleted') IS NULL
                ALTER TABLE [Units] ADD [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Units_IsDeleted] DEFAULT 0;
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_unit_business_branch_name' AND object_id = OBJECT_ID(N'[dbo].[Units]'))
                CREATE UNIQUE INDEX [idx_unit_business_branch_name] ON [dbo].[Units]([BusinessId], [BranchId], [Name]) WHERE [IsDeleted] = 0;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_unit_business_branch_code' AND object_id = OBJECT_ID(N'[dbo].[Units]'))
                CREATE INDEX [idx_unit_business_branch_code] ON [dbo].[Units]([BusinessId], [BranchId], [Code]);
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
                logger.LogWarning(ex, "Unit master schema batch skipped or partially applied.");
            }
        }

        await SeedDefaultUnitsAsync(context, logger);
    }

    private static async Task SeedDefaultUnitsAsync(POSDbContext context, ILogger logger)
    {
        var branch = await context.Branches
            .IgnoreQueryFilters()
            .OrderBy(b => b.Id)
            .Select(b => new { b.Id, b.BusinessId })
            .FirstOrDefaultAsync();

        if (branch == null)
            return;

        var units = new[]
        {
            ("Piece", "PCS", "Single item", 1m),
            ("Box", "BOX", "Box/package", 1m),
            ("Pack", "PACK", "Pack/bundle", 1m),
            ("Kg", "KG", "Kilogram", 1m),
            ("Gram", "G", "Gram", 0.001m),
            ("Liter", "LTR", "Liter", 1m),
            ("Meter", "M", "Meter", 1m)
        };

        foreach (var unit in units)
        {
            try
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    IF NOT EXISTS (
                        SELECT 1 FROM [Units]
                        WHERE [BusinessId] = {branch.BusinessId}
                          AND [BranchId] = {branch.Id}
                          AND [Name] = {unit.Item1}
                          AND [IsDeleted] = 0)
                    BEGIN
                        INSERT INTO [Units] ([Name], [Code], [Description], [ConversionFactor], [Status], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                        VALUES ({unit.Item1}, {unit.Item2}, {unit.Item3}, {unit.Item4}, 1, {branch.BusinessId}, {branch.Id}, GETUTCDATE(), 0);
                    END
                    """);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed unit {UnitName}.", unit.Item1);
            }
        }
    }
}
