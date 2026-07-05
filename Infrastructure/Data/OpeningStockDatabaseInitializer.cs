using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class OpeningStockDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVouchers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[OpeningStockVouchers] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [VoucherNo] NVARCHAR(30) NOT NULL,
                    [VoucherDate] DATETIME2 NOT NULL CONSTRAINT [DF_OpeningStockVouchers_VoucherDate] DEFAULT GETUTCDATE(),
                    [Description] NVARCHAR(500) NULL,
                    [WarehouseId] INT NOT NULL,
                    [TotalAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_OpeningStockVouchers_TotalAmount] DEFAULT 0,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_OpeningStockVouchers_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_OpeningStockVouchers_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_OpeningStockVouchers_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_OpeningStockVouchers_Warehouses] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses]([Id]),
                    CONSTRAINT [FK_OpeningStockVouchers_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
                CREATE UNIQUE INDEX [idx_opening_stock_voucher_no]
                    ON [dbo].[OpeningStockVouchers]([BusinessId], [BranchId], [VoucherNo])
                    WHERE [IsDeleted] = 0;
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[OpeningStockVoucherLines] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [VoucherId] INT NOT NULL,
                    [ProductId] INT NOT NULL,
                    [VariantId] INT NULL,
                    [UnitId] INT NULL,
                    [UnitQuantity] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_OpeningStockVoucherLines_UnitQuantity] DEFAULT 0,
                    [ConversionFactor] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_OpeningStockVoucherLines_ConversionFactor] DEFAULT 1,
                    [Quantity] DECIMAL(18,4) NOT NULL,
                    [CostPrice] DECIMAL(18,4) NOT NULL,
                    [TotalAmount] DECIMAL(18,2) NOT NULL,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_OpeningStockVoucherLines_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_OpeningStockVoucherLines_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_OpeningStockVoucherLines_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_OpeningStockVoucherLines_Vouchers] FOREIGN KEY ([VoucherId]) REFERENCES [dbo].[OpeningStockVouchers]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_OpeningStockVoucherLines_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id])
                );
            END
            """,
            // Legacy table upgrade — one column per batch so partial failures are recoverable.
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'VariantId') IS NULL
                ALTER TABLE [dbo].[OpeningStockVoucherLines] ADD [VariantId] INT NULL;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'UnitId') IS NULL
                ALTER TABLE [dbo].[OpeningStockVoucherLines] ADD [UnitId] INT NULL;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'UnitQuantity') IS NULL
                ALTER TABLE [dbo].[OpeningStockVoucherLines] ADD [UnitQuantity] DECIMAL(18,4) NOT NULL
                    CONSTRAINT [DF_OpeningStockVoucherLines_UnitQuantity_Legacy] DEFAULT 0;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'ConversionFactor') IS NULL
                ALTER TABLE [dbo].[OpeningStockVoucherLines] ADD [ConversionFactor] DECIMAL(18,4) NOT NULL
                    CONSTRAINT [DF_OpeningStockVoucherLines_ConversionFactor_Legacy] DEFAULT 1;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'UnitQuantity') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'Quantity') IS NOT NULL
                UPDATE [dbo].[OpeningStockVoucherLines]
                SET [UnitQuantity] = [Quantity], [ConversionFactor] = 1
                WHERE [UnitQuantity] = 0 AND [Quantity] <> 0;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVouchers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVouchers]', N'IsReversed') IS NULL
                ALTER TABLE [dbo].[OpeningStockVouchers] ADD [IsReversed] BIT NOT NULL
                    CONSTRAINT [DF_OpeningStockVouchers_IsReversed] DEFAULT 0;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVouchers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVouchers]', N'ReversedAt') IS NULL
                ALTER TABLE [dbo].[OpeningStockVouchers] ADD [ReversedAt] DATETIME2 NULL;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVouchers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVouchers]', N'ReversedBy') IS NULL
                ALTER TABLE [dbo].[OpeningStockVouchers] ADD [ReversedBy] INT NULL;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVouchers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVouchers]', N'ReferenceVoucherId') IS NULL
                ALTER TABLE [dbo].[OpeningStockVouchers] ADD [ReferenceVoucherId] INT NULL;
            """,
            """
            IF OBJECT_ID(N'[dbo].[OpeningStockVouchers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[OpeningStockVouchers]', N'ReversalVoucherId') IS NULL
                ALTER TABLE [dbo].[OpeningStockVouchers] ADD [ReversalVoucherId] INT NULL;
            """,
            """
            IF OBJECT_ID(N'[dbo].[StockLedger]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[StockLedger]', N'VoucherId') IS NULL
                ALTER TABLE [dbo].[StockLedger] ADD [VoucherId] INT NULL;
            """,
            """
            IF OBJECT_ID(N'[dbo].[StockLedger]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[StockLedger]', N'VoucherId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'idx_ledger_voucher_type'
                      AND object_id = OBJECT_ID(N'[dbo].[StockLedger]'))
                CREATE INDEX [idx_ledger_voucher_type] ON [dbo].[StockLedger]([VoucherId], [Type]);
            """,
            """
            IF OBJECT_ID(N'[dbo].[StockLedger]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[OpeningStockVouchers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[StockLedger]', N'VoucherId') IS NOT NULL
                UPDATE sl
                SET sl.[VoucherId] = sl.[ReferenceId]
                FROM [dbo].[StockLedger] sl
                INNER JOIN [dbo].[OpeningStockVouchers] osv ON osv.[Id] = sl.[ReferenceId]
                WHERE sl.[VoucherId] IS NULL
                  AND sl.[Type] IN (10, 11)
                  AND sl.[IsDeleted] = 0;
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
                logger.LogError(ex, "Opening stock voucher schema batch failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}
