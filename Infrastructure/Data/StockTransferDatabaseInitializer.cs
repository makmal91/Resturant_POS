using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class StockTransferDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[StockTransferVouchers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[StockTransferVouchers] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [TransferNo] NVARCHAR(30) NOT NULL,
                    [TransferDate] DATETIME2 NOT NULL CONSTRAINT [DF_StockTransferVouchers_TransferDate] DEFAULT GETUTCDATE(),
                    [Description] NVARCHAR(500) NULL,
                    [FromWarehouseId] INT NOT NULL,
                    [ToWarehouseId] INT NOT NULL,
                    [IsReversed] BIT NOT NULL CONSTRAINT [DF_StockTransferVouchers_IsReversed] DEFAULT 0,
                    [ReversedAt] DATETIME2 NULL,
                    [ReversedBy] INT NULL,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_StockTransferVouchers_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_StockTransferVouchers_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_StockTransferVouchers_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_StockTransferVouchers_FromWarehouse] FOREIGN KEY ([FromWarehouseId]) REFERENCES [dbo].[Warehouses]([Id]),
                    CONSTRAINT [FK_StockTransferVouchers_ToWarehouse] FOREIGN KEY ([ToWarehouseId]) REFERENCES [dbo].[Warehouses]([Id]),
                    CONSTRAINT [FK_StockTransferVouchers_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
                CREATE UNIQUE INDEX [idx_stock_transfer_no]
                    ON [dbo].[StockTransferVouchers]([BusinessId], [BranchId], [TransferNo])
                    WHERE [IsDeleted] = 0;
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[StockTransferVoucherLines]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[StockTransferVoucherLines] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [VoucherId] INT NOT NULL,
                    [ProductId] INT NOT NULL,
                    [VariantId] INT NULL,
                    [UnitId] INT NULL,
                    [UnitQuantity] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_StockTransferVoucherLines_UnitQuantity] DEFAULT 0,
                    [ConversionFactor] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_StockTransferVoucherLines_ConversionFactor] DEFAULT 1,
                    [Quantity] DECIMAL(18,4) NOT NULL,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_StockTransferVoucherLines_BusinessId] DEFAULT 1,
                    [BranchId] INT NOT NULL,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_StockTransferVoucherLines_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_StockTransferVoucherLines_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_StockTransferVoucherLines_Vouchers] FOREIGN KEY ([VoucherId]) REFERENCES [dbo].[StockTransferVouchers]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_StockTransferVoucherLines_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id])
                );
            END
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
                logger.LogError(ex, "Stock transfer schema batch failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}
