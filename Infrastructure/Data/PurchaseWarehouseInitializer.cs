using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class PurchaseWarehouseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[Warehouses]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Warehouses] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(150) NOT NULL,
                    [Code] NVARCHAR(30) NOT NULL CONSTRAINT [DF_Warehouses_Code] DEFAULT N'',
                    [Address] NVARCHAR(500) NOT NULL CONSTRAINT [DF_Warehouses_Address] DEFAULT N'',
                    [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouses_IsActive] DEFAULT 1,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_Warehouses_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Warehouses_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Warehouses_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_Warehouses_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            SyncLegacyWarehousesSchemaSql(),
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_warehouse_business_branch_name' AND object_id = OBJECT_ID(N'[dbo].[Warehouses]'))
                CREATE UNIQUE INDEX [idx_warehouse_business_branch_name] ON [dbo].[Warehouses]([BusinessId], [BranchId], [Name]) WHERE [IsDeleted] = 0;
            """,
            """
            IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Suppliers] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SupplierCode] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Suppliers_SupplierCode] DEFAULT N'',
                    [Name] NVARCHAR(200) NOT NULL,
                    [ContactPerson] NVARCHAR(150) NOT NULL CONSTRAINT [DF_Suppliers_ContactPerson] DEFAULT N'',
                    [Phone] NVARCHAR(30) NOT NULL CONSTRAINT [DF_Suppliers_Phone] DEFAULT N'',
                    [Email] NVARCHAR(150) NOT NULL CONSTRAINT [DF_Suppliers_Email] DEFAULT N'',
                    [Address] NVARCHAR(500) NOT NULL CONSTRAINT [DF_Suppliers_Address] DEFAULT N'',
                    [TaxNumber] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Suppliers_TaxNumber] DEFAULT N'',
                    [IsActive] BIT NOT NULL CONSTRAINT [DF_Suppliers_IsActive] DEFAULT 1,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_Suppliers_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Suppliers_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Suppliers_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_Suppliers_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            SyncLegacySuppliersColumnsSql(),
            SyncLegacySuppliersCodeBackfillSql(),
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_supplier_business_branch_name' AND object_id = OBJECT_ID(N'[dbo].[Suppliers]'))
                CREATE INDEX [idx_supplier_business_branch_name] ON [dbo].[Suppliers]([BusinessId], [BranchId], [Name]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_supplier_branch_code' AND object_id = OBJECT_ID(N'[dbo].[Suppliers]'))
               AND COL_LENGTH(N'dbo.Suppliers', N'SupplierCode') IS NOT NULL
                CREATE UNIQUE INDEX [idx_supplier_branch_code]
                    ON [dbo].[Suppliers]([BusinessId], [BranchId], [SupplierCode])
                    WHERE [SupplierCode] <> '' AND [IsDeleted] = 0;
            """,
            """
            IF OBJECT_ID(N'[dbo].[Purchases]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Purchases] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [InvoiceNo] NVARCHAR(100) NOT NULL,
                    [SupplierId] INT NOT NULL,
                    [WarehouseId] INT NOT NULL,
                    [PurchaseDate] DATETIME2 NOT NULL CONSTRAINT [DF_Purchases_PurchaseDate] DEFAULT GETUTCDATE(),
                    [TotalAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Purchases_TotalAmount] DEFAULT 0,
                    [Status] INT NOT NULL CONSTRAINT [DF_Purchases_Status] DEFAULT 0,
                    [Notes] NVARCHAR(1000) NOT NULL CONSTRAINT [DF_Purchases_Notes] DEFAULT N'',
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_Purchases_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Purchases_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Purchases_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_Purchases_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers]([Id]),
                    CONSTRAINT [FK_Purchases_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses]([Id]),
                    CONSTRAINT [FK_Purchases_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            SyncLegacyPurchasesSchemaSql(),
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_purchase_business_branch_invoice' AND object_id = OBJECT_ID(N'[dbo].[Purchases]'))
               AND COL_LENGTH(N'dbo.Purchases', N'InvoiceNo') IS NOT NULL
                CREATE UNIQUE INDEX [idx_purchase_business_branch_invoice] ON [dbo].[Purchases]([BusinessId], [BranchId], [InvoiceNo]) WHERE [IsDeleted] = 0;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_purchase_business_branch_status' AND object_id = OBJECT_ID(N'[dbo].[Purchases]'))
                CREATE INDEX [idx_purchase_business_branch_status] ON [dbo].[Purchases]([BusinessId], [BranchId], [Status]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_purchase_business_branch_date' AND object_id = OBJECT_ID(N'[dbo].[Purchases]'))
                CREATE INDEX [idx_purchase_business_branch_date] ON [dbo].[Purchases]([BusinessId], [BranchId], [PurchaseDate] DESC);
            """,
            """
            IF OBJECT_ID(N'[dbo].[PurchaseItems]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PurchaseItems] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [PurchaseId] INT NOT NULL,
                    [ProductId] INT NOT NULL,
                    [VariantId] INT NULL,
                    [UnitId] INT NOT NULL,
                    [Quantity] DECIMAL(18,4) NOT NULL,
                    [ConversionFactor] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_PurchaseItems_ConversionFactor] DEFAULT 1,
                    [BaseQuantity] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_PurchaseItems_BaseQuantity] DEFAULT 0,
                    [CostPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_PurchaseItems_CostPrice] DEFAULT 0,
                    [TotalCost] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_PurchaseItems_TotalCost] DEFAULT 0,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_PurchaseItems_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_PurchaseItems_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_PurchaseItems_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_PurchaseItems_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [dbo].[Purchases]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_PurchaseItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
                    CONSTRAINT [FK_PurchaseItems_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [dbo].[ProductVariants]([Id]),
                    CONSTRAINT [FK_PurchaseItems_ProductUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [dbo].[ProductUnits]([Id])
                );
            END
            """,
            SyncLegacyPurchaseItemsSchemaSql(),
            """
            IF OBJECT_ID(N'[dbo].[StockLedger]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[StockLedger] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProductId] INT NOT NULL,
                    [VariantId] INT NULL,
                    [WarehouseId] INT NOT NULL,
                    [Type] INT NOT NULL,
                    [ReferenceId] INT NULL,
                    [VoucherId] INT NULL,
                    [QuantityInBaseUnit] DECIMAL(18,4) NOT NULL,
                    [UnitPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_StockLedger_UnitPrice] DEFAULT 0,
                    [TotalAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_StockLedger_TotalAmount] DEFAULT 0,
                    [Date] DATETIME2 NOT NULL CONSTRAINT [DF_StockLedger_Date] DEFAULT GETUTCDATE(),
                    [Remarks] NVARCHAR(500) NOT NULL CONSTRAINT [DF_StockLedger_Remarks] DEFAULT N'',
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_StockLedger_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_StockLedger_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_StockLedger_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_StockLedger_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]),
                    CONSTRAINT [FK_StockLedger_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [dbo].[ProductVariants]([Id]),
                    CONSTRAINT [FK_StockLedger_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses]([Id])
                );
            END
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_ledger_business_branch_product_warehouse' AND object_id = OBJECT_ID(N'[dbo].[StockLedger]'))
                CREATE INDEX [idx_ledger_business_branch_product_warehouse] ON [dbo].[StockLedger]([BusinessId], [BranchId], [ProductId], [WarehouseId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_ledger_business_branch_product_variant_warehouse' AND object_id = OBJECT_ID(N'[dbo].[StockLedger]'))
                CREATE INDEX [idx_ledger_business_branch_product_variant_warehouse] ON [dbo].[StockLedger]([BusinessId], [BranchId], [ProductId], [VariantId], [WarehouseId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_ledger_business_branch_date' AND object_id = OBJECT_ID(N'[dbo].[StockLedger]'))
                CREATE INDEX [idx_ledger_business_branch_date] ON [dbo].[StockLedger]([BusinessId], [BranchId], [Date] DESC);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_ledger_reference' AND object_id = OBJECT_ID(N'[dbo].[StockLedger]'))
                CREATE INDEX [idx_ledger_reference] ON [dbo].[StockLedger]([ReferenceId], [BusinessId], [BranchId], [Type]);
            """,
            SyncStockLedgerUnitColumnsSql(),
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Purchases]') AND name = N'VoidedAt')
                ALTER TABLE [dbo].[Purchases] ADD [VoidedAt] DATETIME2 NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Purchases]') AND name = N'VoidedByName')
                ALTER TABLE [dbo].[Purchases] ADD [VoidedByName] NVARCHAR(200) NULL;
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
                logger.LogWarning(ex, "Purchase/Warehouse schema batch skipped or partially applied.");
            }
        }
    }

    private static string SyncLegacyWarehousesSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[Warehouses]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Warehouses', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[Warehouses] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.Warehouses', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[Warehouses] ADD [ModifiedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.Warehouses', N'Address') IS NOT NULL
               AND EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.Warehouses')
                      AND name = N'Address'
                      AND is_nullable = 1)
                UPDATE [dbo].[Warehouses] SET [Address] = N'' WHERE [Address] IS NULL;
        END
        """;

    private static string SyncLegacySuppliersColumnsSql() => """
        IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Suppliers', N'SupplierCode') IS NULL
               AND COL_LENGTH(N'dbo.Suppliers', N'Code') IS NOT NULL
                EXEC sp_rename N'dbo.Suppliers.Code', N'SupplierCode', N'COLUMN';

            IF COL_LENGTH(N'dbo.Suppliers', N'SupplierCode') IS NULL
                ALTER TABLE [dbo].[Suppliers] ADD [SupplierCode] NVARCHAR(50) NOT NULL
                    CONSTRAINT [DF_Suppliers_SupplierCode_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Suppliers', N'ContactPerson') IS NULL
                ALTER TABLE [dbo].[Suppliers] ADD [ContactPerson] NVARCHAR(150) NOT NULL
                    CONSTRAINT [DF_Suppliers_ContactPerson_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Suppliers', N'TaxNumber') IS NULL
                ALTER TABLE [dbo].[Suppliers] ADD [TaxNumber] NVARCHAR(50) NOT NULL
                    CONSTRAINT [DF_Suppliers_TaxNumber_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Suppliers', N'Phone') IS NULL
                ALTER TABLE [dbo].[Suppliers] ADD [Phone] NVARCHAR(30) NOT NULL
                    CONSTRAINT [DF_Suppliers_Phone_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Suppliers', N'Email') IS NULL
                ALTER TABLE [dbo].[Suppliers] ADD [Email] NVARCHAR(150) NOT NULL
                    CONSTRAINT [DF_Suppliers_Email_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Suppliers', N'Address') IS NULL
                ALTER TABLE [dbo].[Suppliers] ADD [Address] NVARCHAR(500) NOT NULL
                    CONSTRAINT [DF_Suppliers_Address_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Suppliers', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[Suppliers] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.Suppliers', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[Suppliers] ADD [ModifiedByName] NVARCHAR(MAX) NULL;
        END
        """;

    private static string SyncLegacySuppliersCodeBackfillSql() => """
        SET QUOTED_IDENTIFIER ON;
        IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.Suppliers', N'Code') IS NOT NULL
           AND COL_LENGTH(N'dbo.Suppliers', N'SupplierCode') IS NOT NULL
            EXEC(N'
                UPDATE [dbo].[Suppliers]
                SET [SupplierCode] = [Code]
                WHERE ([SupplierCode] IS NULL OR [SupplierCode] = N'''')
                  AND [Code] IS NOT NULL AND [Code] <> N'''';
            ');
        """;

    private static string SyncLegacyPurchasesSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[Purchases]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Purchases', N'InvoiceNo') IS NULL
               AND COL_LENGTH(N'dbo.Purchases', N'PurchaseNumber') IS NOT NULL
                EXEC sp_rename N'dbo.Purchases.PurchaseNumber', N'InvoiceNo', N'COLUMN';

            IF COL_LENGTH(N'dbo.Purchases', N'WarehouseId') IS NULL
                ALTER TABLE [dbo].[Purchases] ADD [WarehouseId] INT NOT NULL
                    CONSTRAINT [DF_Purchases_WarehouseId_Legacy] DEFAULT 1;

            IF COL_LENGTH(N'dbo.Purchases', N'InvoiceNo') IS NULL
                ALTER TABLE [dbo].[Purchases] ADD [InvoiceNo] NVARCHAR(100) NOT NULL
                    CONSTRAINT [DF_Purchases_InvoiceNo_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Purchases', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[Purchases] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.Purchases', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[Purchases] ADD [ModifiedByName] NVARCHAR(MAX) NULL;
        END
        """;

    private static string SyncLegacyPurchaseItemsSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[PurchaseItems]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.PurchaseItems', N'CostPrice') IS NULL
               AND COL_LENGTH(N'dbo.PurchaseItems', N'UnitPrice') IS NOT NULL
                EXEC sp_rename N'dbo.PurchaseItems.UnitPrice', N'CostPrice', N'COLUMN';

            IF COL_LENGTH(N'dbo.PurchaseItems', N'TotalCost') IS NULL
               AND COL_LENGTH(N'dbo.PurchaseItems', N'TotalPrice') IS NOT NULL
                EXEC sp_rename N'dbo.PurchaseItems.TotalPrice', N'TotalCost', N'COLUMN';

            IF COL_LENGTH(N'dbo.PurchaseItems', N'VariantId') IS NULL
                ALTER TABLE [dbo].[PurchaseItems] ADD [VariantId] INT NULL;

            IF COL_LENGTH(N'dbo.PurchaseItems', N'ConversionFactor') IS NULL
                ALTER TABLE [dbo].[PurchaseItems] ADD [ConversionFactor] DECIMAL(18,4) NOT NULL
                    CONSTRAINT [DF_PurchaseItems_ConversionFactor_Legacy] DEFAULT 1;

            IF COL_LENGTH(N'dbo.PurchaseItems', N'BaseQuantity') IS NULL
                ALTER TABLE [dbo].[PurchaseItems] ADD [BaseQuantity] DECIMAL(18,4) NOT NULL
                    CONSTRAINT [DF_PurchaseItems_BaseQuantity_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.PurchaseItems', N'CostPrice') IS NULL
                ALTER TABLE [dbo].[PurchaseItems] ADD [CostPrice] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_PurchaseItems_CostPrice_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.PurchaseItems', N'TotalCost') IS NULL
                ALTER TABLE [dbo].[PurchaseItems] ADD [TotalCost] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_PurchaseItems_TotalCost_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.PurchaseItems', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[PurchaseItems] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.PurchaseItems', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[PurchaseItems] ADD [ModifiedByName] NVARCHAR(MAX) NULL;
        END
        """;

    private static string SyncStockLedgerUnitColumnsSql() => """
        IF OBJECT_ID(N'[dbo].[StockLedger]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.StockLedger', N'UnitId') IS NULL
                ALTER TABLE [dbo].[StockLedger] ADD [UnitId] INT NULL;

            IF COL_LENGTH(N'dbo.StockLedger', N'UnitQuantity') IS NULL
                ALTER TABLE [dbo].[StockLedger] ADD [UnitQuantity] DECIMAL(18,4) NULL;

            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = N'FK_StockLedger_ProductUnits_UnitId'
                  AND parent_object_id = OBJECT_ID(N'[dbo].[StockLedger]'))
               AND COL_LENGTH(N'dbo.StockLedger', N'UnitId') IS NOT NULL
                ALTER TABLE [dbo].[StockLedger]
                    ADD CONSTRAINT [FK_StockLedger_ProductUnits_UnitId]
                    FOREIGN KEY ([UnitId]) REFERENCES [dbo].[ProductUnits]([Id]);

            IF COL_LENGTH(N'dbo.StockLedger', N'VoucherId') IS NULL
                ALTER TABLE [dbo].[StockLedger] ADD [VoucherId] INT NULL;

            IF COL_LENGTH(N'dbo.StockLedger', N'VoucherId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'idx_ledger_voucher_type'
                      AND object_id = OBJECT_ID(N'[dbo].[StockLedger]'))
                CREATE INDEX [idx_ledger_voucher_type] ON [dbo].[StockLedger]([VoucherId], [Type]);

            IF OBJECT_ID(N'[dbo].[OpeningStockVouchers]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.StockLedger', N'VoucherId') IS NOT NULL
                UPDATE sl
                SET sl.[VoucherId] = sl.[ReferenceId]
                FROM [dbo].[StockLedger] sl
                INNER JOIN [dbo].[OpeningStockVouchers] osv ON osv.[Id] = sl.[ReferenceId]
                WHERE sl.[VoucherId] IS NULL
                  AND sl.[Type] IN (10, 11)
                  AND sl.[IsDeleted] = 0;
        END
        """;
}
