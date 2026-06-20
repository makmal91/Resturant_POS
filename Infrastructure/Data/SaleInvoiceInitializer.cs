using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class SaleInvoiceInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[SaleInvoices]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SaleInvoices] (
                    [Id]             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [InvoiceNo]      NVARCHAR(100) NOT NULL,
                    [CustomerId]     INT NULL,
                    [WarehouseId]    INT NOT NULL,
                    [SaleDate]       DATETIME2 NOT NULL CONSTRAINT [DF_SaleInvoices_SaleDate]       DEFAULT GETUTCDATE(),
                    [SubTotal]       DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoices_SubTotal]       DEFAULT 0,
                    [DiscountAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoices_DiscountAmount] DEFAULT 0,
                    [TaxAmount]      DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoices_TaxAmount]      DEFAULT 0,
                    [GrandTotal]     DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoices_GrandTotal]     DEFAULT 0,
                    [PaidAmount]     DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoices_PaidAmount]     DEFAULT 0,
                    [ReturnAmount]   DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoices_ReturnAmount]   DEFAULT 0,
                    [PaymentMethod]  INT NOT NULL CONSTRAINT [DF_SaleInvoices_PaymentMethod]  DEFAULT 1,
                    [CashAmount]     DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoices_CashAmount]     DEFAULT 0,
                    [CardAmount]     DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoices_CardAmount]     DEFAULT 0,
                    [Status]         INT NOT NULL CONSTRAINT [DF_SaleInvoices_Status]         DEFAULT 0,
                    [PricingType]    INT NOT NULL CONSTRAINT [DF_SaleInvoices_PricingType]    DEFAULT 1,
                    [Notes]          NVARCHAR(1000) NULL,
                    [HeldNote]       NVARCHAR(500)  NULL,
                    [CashierName]    NVARCHAR(200)  NULL,
                    [BusinessId]     INT NOT NULL CONSTRAINT [DF_SaleInvoices_BusinessId]     DEFAULT 1,
                    [BranchId]       INT NOT NULL,
                    [CreatedDate]    DATETIME2 NOT NULL CONSTRAINT [DF_SaleInvoices_CreatedDate]    DEFAULT GETUTCDATE(),
                    [CreatedById]    INT NULL,
                    [CreatedByName]  NVARCHAR(MAX) NULL,
                    [UpdatedDate]    DATETIME2 NULL,
                    [ModifiedById]   INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted]      BIT NOT NULL CONSTRAINT [DF_SaleInvoices_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_SaleInvoices_Customers_CustomerId]   FOREIGN KEY ([CustomerId])   REFERENCES [dbo].[Customers]([Id]),
                    CONSTRAINT [FK_SaleInvoices_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId])  REFERENCES [dbo].[Warehouses]([Id]),
                    CONSTRAINT [FK_SaleInvoices_Branches_BranchId]      FOREIGN KEY ([BranchId])     REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            SyncLegacySaleInvoiceSchemaSql(),
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_saleinvoices_business_branch_invoiceno' AND object_id = OBJECT_ID(N'[dbo].[SaleInvoices]'))
                CREATE UNIQUE INDEX [idx_saleinvoices_business_branch_invoiceno]
                    ON [dbo].[SaleInvoices]([BusinessId], [BranchId], [InvoiceNo]) WHERE [IsDeleted] = 0;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_saleinvoices_business_branch_status' AND object_id = OBJECT_ID(N'[dbo].[SaleInvoices]'))
                CREATE INDEX [idx_saleinvoices_business_branch_status]
                    ON [dbo].[SaleInvoices]([BusinessId], [BranchId], [Status]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_saleinvoices_business_branch_saledate' AND object_id = OBJECT_ID(N'[dbo].[SaleInvoices]'))
                CREATE INDEX [idx_saleinvoices_business_branch_saledate]
                    ON [dbo].[SaleInvoices]([BusinessId], [BranchId], [SaleDate] DESC);
            """,
            """
            IF OBJECT_ID(N'[dbo].[SaleInvoiceItems]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SaleInvoiceItems] (
                    [Id]              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SaleInvoiceId]   INT NOT NULL,
                    [ProductId]       INT NOT NULL,
                    [VariantId]       INT NULL,
                    [UnitId]          INT NOT NULL,
                    [Quantity]        DECIMAL(18,4) NOT NULL,
                    [UnitPrice]       DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoiceItems_UnitPrice]       DEFAULT 0,
                    [DiscountPercent] DECIMAL(8,4)  NOT NULL CONSTRAINT [DF_SaleInvoiceItems_DiscountPercent] DEFAULT 0,
                    [DiscountAmount]  DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoiceItems_DiscountAmount]  DEFAULT 0,
                    [TaxPercent]      DECIMAL(8,4)  NOT NULL CONSTRAINT [DF_SaleInvoiceItems_TaxPercent]      DEFAULT 0,
                    [TaxAmount]       DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoiceItems_TaxAmount]       DEFAULT 0,
                    [LineTotal]       DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SaleInvoiceItems_LineTotal]       DEFAULT 0,
                    [ItemNote]        NVARCHAR(500) NULL,
                    [BusinessId]      INT NOT NULL CONSTRAINT [DF_SaleInvoiceItems_BusinessId] DEFAULT 1,
                    [BranchId]        INT NOT NULL,
                    [CreatedDate]     DATETIME2 NOT NULL CONSTRAINT [DF_SaleInvoiceItems_CreatedDate]    DEFAULT GETUTCDATE(),
                    [CreatedById]     INT NULL,
                    [CreatedByName]   NVARCHAR(MAX) NULL,
                    [UpdatedDate]     DATETIME2 NULL,
                    [ModifiedById]    INT NULL,
                    [ModifiedByName]  NVARCHAR(MAX) NULL,
                    [IsDeleted]       BIT NOT NULL CONSTRAINT [DF_SaleInvoiceItems_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_SaleInvoiceItems_SaleInvoices_SaleInvoiceId] FOREIGN KEY ([SaleInvoiceId]) REFERENCES [dbo].[SaleInvoices]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_SaleInvoiceItems_Products_ProductId]         FOREIGN KEY ([ProductId])    REFERENCES [dbo].[Products]([Id]),
                    CONSTRAINT [FK_SaleInvoiceItems_ProductVariants_VariantId]  FOREIGN KEY ([VariantId])    REFERENCES [dbo].[ProductVariants]([Id]),
                    CONSTRAINT [FK_SaleInvoiceItems_ProductUnits_UnitId]        FOREIGN KEY ([UnitId])       REFERENCES [dbo].[ProductUnits]([Id]),
                    CONSTRAINT [FK_SaleInvoiceItems_Branches_BranchId]          FOREIGN KEY ([BranchId])     REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            SyncLegacySaleInvoiceItemSchemaSql(),
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleInvoiceItems]') AND name = N'ConversionFactor')
                ALTER TABLE [dbo].[SaleInvoiceItems]
                    ADD [ConversionFactor] DECIMAL(18,6) NOT NULL CONSTRAINT [DF_SaleInvoiceItems_ConversionFactor] DEFAULT 1;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleInvoiceItems]') AND name = N'BaseQuantity')
                ALTER TABLE [dbo].[SaleInvoiceItems]
                    ADD [BaseQuantity] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_SaleInvoiceItems_BaseQuantity] DEFAULT 0;
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleInvoices]') AND name = N'VoidedAt')
                ALTER TABLE [dbo].[SaleInvoices] ADD [VoidedAt] DATETIME2 NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleInvoices]') AND name = N'VoidedByName')
                ALTER TABLE [dbo].[SaleInvoices] ADD [VoidedByName] NVARCHAR(200) NULL;
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
                logger.LogWarning(ex, "SaleInvoice schema batch skipped or partially applied.");
            }
        }
    }

    private static string SyncLegacySaleInvoiceSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[SaleInvoices]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.SaleInvoices', N'InvoiceNo') IS NULL
               AND COL_LENGTH(N'dbo.SaleInvoices', N'InvoiceNumber') IS NOT NULL
                EXEC sp_rename N'dbo.SaleInvoices.InvoiceNumber', N'InvoiceNo', N'COLUMN';

            IF COL_LENGTH(N'dbo.SaleInvoices', N'SaleDate') IS NULL
               AND COL_LENGTH(N'dbo.SaleInvoices', N'InvoiceDate') IS NOT NULL
                EXEC sp_rename N'dbo.SaleInvoices.InvoiceDate', N'SaleDate', N'COLUMN';

            IF COL_LENGTH(N'dbo.SaleInvoices', N'GrandTotal') IS NULL
               AND COL_LENGTH(N'dbo.SaleInvoices', N'TotalAmount') IS NOT NULL
                EXEC sp_rename N'dbo.SaleInvoices.TotalAmount', N'GrandTotal', N'COLUMN';

            IF COL_LENGTH(N'dbo.SaleInvoices', N'WarehouseId') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [WarehouseId] INT NOT NULL
                    CONSTRAINT [DF_SaleInvoices_WarehouseId_Legacy] DEFAULT 1;

            IF COL_LENGTH(N'dbo.SaleInvoices', N'ReturnAmount') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [ReturnAmount] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_SaleInvoices_ReturnAmount_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.SaleInvoices', N'CashAmount') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [CashAmount] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_SaleInvoices_CashAmount_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.SaleInvoices', N'CardAmount') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [CardAmount] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_SaleInvoices_CardAmount_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.SaleInvoices', N'PricingType') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [PricingType] INT NOT NULL
                    CONSTRAINT [DF_SaleInvoices_PricingType_Legacy] DEFAULT 1;

            IF COL_LENGTH(N'dbo.SaleInvoices', N'HeldNote') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [HeldNote] NVARCHAR(500) NULL;

            IF COL_LENGTH(N'dbo.SaleInvoices', N'CashierName') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [CashierName] NVARCHAR(200) NULL;

            IF COL_LENGTH(N'dbo.SaleInvoices', N'SaleDate') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [SaleDate] DATETIME2 NOT NULL
                    CONSTRAINT [DF_SaleInvoices_SaleDate_Legacy] DEFAULT GETUTCDATE();

            IF COL_LENGTH(N'dbo.SaleInvoices', N'GrandTotal') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [GrandTotal] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_SaleInvoices_GrandTotal_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.SaleInvoices', N'InvoiceNo') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [InvoiceNo] NVARCHAR(100) NOT NULL
                    CONSTRAINT [DF_SaleInvoices_InvoiceNo_Legacy] DEFAULT N'';
        END
        """;

    private static string SyncLegacySaleInvoiceItemSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[SaleInvoiceItems]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.SaleInvoiceItems', N'LineTotal') IS NULL
               AND COL_LENGTH(N'dbo.SaleInvoiceItems', N'TotalPrice') IS NOT NULL
                EXEC sp_rename N'dbo.SaleInvoiceItems.TotalPrice', N'LineTotal', N'COLUMN';

            IF COL_LENGTH(N'dbo.SaleInvoiceItems', N'VariantId') IS NULL
                ALTER TABLE [dbo].[SaleInvoiceItems] ADD [VariantId] INT NULL;

            IF COL_LENGTH(N'dbo.SaleInvoiceItems', N'DiscountAmount') IS NULL
                ALTER TABLE [dbo].[SaleInvoiceItems] ADD [DiscountAmount] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_SaleInvoiceItems_DiscountAmount_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.SaleInvoiceItems', N'TaxAmount') IS NULL
                ALTER TABLE [dbo].[SaleInvoiceItems] ADD [TaxAmount] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_SaleInvoiceItems_TaxAmount_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.SaleInvoiceItems', N'ItemNote') IS NULL
                ALTER TABLE [dbo].[SaleInvoiceItems] ADD [ItemNote] NVARCHAR(500) NULL;

            IF COL_LENGTH(N'dbo.SaleInvoiceItems', N'LineTotal') IS NULL
                ALTER TABLE [dbo].[SaleInvoiceItems] ADD [LineTotal] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_SaleInvoiceItems_LineTotal_Legacy] DEFAULT 0;
        END
        """;
}
