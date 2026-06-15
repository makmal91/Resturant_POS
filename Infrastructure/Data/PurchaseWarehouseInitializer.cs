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
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_warehouse_business_branch_name' AND object_id = OBJECT_ID(N'[dbo].[Warehouses]'))
                CREATE UNIQUE INDEX [idx_warehouse_business_branch_name] ON [dbo].[Warehouses]([BusinessId], [BranchId], [Name]) WHERE [IsDeleted] = 0;
            """,
            """
            IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Suppliers] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
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
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_supplier_business_branch_name' AND object_id = OBJECT_ID(N'[dbo].[Suppliers]'))
                CREATE INDEX [idx_supplier_business_branch_name] ON [dbo].[Suppliers]([BusinessId], [BranchId], [Name]);
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
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_purchase_business_branch_invoice' AND object_id = OBJECT_ID(N'[dbo].[Purchases]'))
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
}
