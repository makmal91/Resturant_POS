using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Common.Constants;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Data;

public static class StockAdjustmentDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[AdjustmentTypes]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[AdjustmentTypes] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(100) NOT NULL,
                    [ExpenseAccountId] INT NOT NULL,
                    [IncomeAccountId] INT NOT NULL,
                    [IsActive] BIT NOT NULL CONSTRAINT [DF_AdjustmentTypes_IsActive] DEFAULT 1,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_AdjustmentTypes_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_AdjustmentTypes_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_AdjustmentTypes_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_AdjustmentTypes_ExpenseAccount] FOREIGN KEY ([ExpenseAccountId]) REFERENCES [dbo].[Accounts]([Id]),
                    CONSTRAINT [FK_AdjustmentTypes_IncomeAccount] FOREIGN KEY ([IncomeAccountId]) REFERENCES [dbo].[Accounts]([Id]),
                    CONSTRAINT [FK_AdjustmentTypes_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
                CREATE UNIQUE INDEX [UX_AdjustmentTypes_Branch_Name]
                    ON [dbo].[AdjustmentTypes]([BusinessId], [BranchId], [Name]) WHERE [IsDeleted] = 0;
            END

            IF OBJECT_ID(N'[dbo].[StockAdjustments]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[StockAdjustments] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [AdjustmentNo] NVARCHAR(50) NOT NULL,
                    [AdjustmentDate] DATETIME2 NOT NULL CONSTRAINT [DF_StockAdjustments_Date] DEFAULT GETUTCDATE(),
                    [WarehouseId] INT NOT NULL,
                    [AdjustmentTypeId] INT NOT NULL,
                    [Remarks] NVARCHAR(500) NULL,
                    [TotalAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_StockAdjustments_TotalAmount] DEFAULT 0,
                    [IsReversed] BIT NOT NULL CONSTRAINT [DF_StockAdjustments_IsReversed] DEFAULT 0,
                    [ReversedAt] DATETIME2 NULL,
                    [ReversedBy] INT NULL,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_StockAdjustments_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_StockAdjustments_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_StockAdjustments_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_StockAdjustments_Warehouse] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses]([Id]),
                    CONSTRAINT [FK_StockAdjustments_Type] FOREIGN KEY ([AdjustmentTypeId]) REFERENCES [dbo].[AdjustmentTypes]([Id]),
                    CONSTRAINT [FK_StockAdjustments_Branch] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
                CREATE UNIQUE INDEX [UX_StockAdjustments_No]
                    ON [dbo].[StockAdjustments]([BusinessId], [BranchId], [AdjustmentNo]) WHERE [IsDeleted] = 0;
            END

            IF OBJECT_ID(N'[dbo].[StockAdjustmentDetails]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[StockAdjustmentDetails] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [StockAdjustmentId] INT NOT NULL,
                    [ProductId] INT NOT NULL,
                    [VariantId] INT NULL,
                    [UnitId] INT NOT NULL,
                    [UnitQuantity] DECIMAL(18,4) NOT NULL,
                    [ConversionFactor] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_StockAdjustmentDetails_ConversionFactor] DEFAULT 1,
                    [BaseQuantity] DECIMAL(18,4) NOT NULL,
                    [CostPrice] DECIMAL(18,4) NOT NULL,
                    [TotalCost] DECIMAL(18,2) NOT NULL,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_StockAdjustmentDetails_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_StockAdjustmentDetails_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_StockAdjustmentDetails_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_StockAdjustmentDetails_Header] FOREIGN KEY ([StockAdjustmentId]) REFERENCES [dbo].[StockAdjustments]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_StockAdjustmentDetails_Product] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
                    CONSTRAINT [FK_StockAdjustmentDetails_Unit] FOREIGN KEY ([UnitId]) REFERENCES [dbo].[ProductUnits]([Id])
                );
            END
            """);

        await SeedDefaultAdjustmentTypesAsync(context, logger);
        logger.LogInformation("Stock adjustment schema ensured.");
    }

    private static async Task SeedDefaultAdjustmentTypesAsync(POSDbContext context, ILogger logger)
    {
        var branches = await context.Branches.AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Select(b => new { b.Id, b.BusinessId })
            .ToListAsync();

        if (branches.Count == 0)
            return;

        var expenseId = await context.GlAccounts.AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive && a.Name == GlAccountDefaults.GeneralExpense)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();

        var incomeId = await context.GlAccounts.AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive && a.Name == GlAccountDefaults.Sales)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();

        if (expenseId <= 0 || incomeId <= 0)
        {
            logger.LogWarning("Skipped adjustment type seed — GL accounts not found.");
            return;
        }

        var defaults = new[] { "Damage", "Expiry", "Theft", "Manual Correction" };

        foreach (var branch in branches)
        {
            foreach (var name in defaults)
            {
                var exists = await context.AdjustmentTypes
                    .IgnoreQueryFilters()
                    .AnyAsync(t => t.BusinessId == branch.BusinessId
                                   && t.BranchId == branch.Id
                                   && t.Name == name
                                   && !t.IsDeleted);

                if (exists)
                    continue;

                await context.AdjustmentTypes.AddAsync(new AdjustmentType
                {
                    Name = name,
                    ExpenseAccountId = expenseId,
                    IncomeAccountId = incomeId,
                    IsActive = true,
                    BusinessId = branch.BusinessId,
                    BranchId = branch.Id
                });
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Default adjustment types seeded.");
        }
    }
}
