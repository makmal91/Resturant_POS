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
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[PermissionModules] WHERE [ModuleName] = N'Sales')
                INSERT INTO [dbo].[PermissionModules] ([ModuleName], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                VALUES (N'Sales', 1, 1, GETUTCDATE(), 0);
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Route] = N'/pos' AND [IsDeleted] = 0)
            BEGIN
                DECLARE @opsGroupId INT;
                SELECT TOP 1 @opsGroupId = [Id] FROM [dbo].[Menus]
                WHERE [Name] = N'Operations' AND [ParentId] IS NULL AND [IsDeleted] = 0;
                IF @opsGroupId IS NOT NULL
                    INSERT INTO [dbo].[Menus] ([Name], [Route], [Icon], [ModuleName], [ParentId], [DisplayOrder], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                    VALUES (N'POS Billing', N'/pos', N'🏪', N'POS Billing', @opsGroupId, 0, 1, 1, GETUTCDATE(), 0);
            END
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Route] = N'/sales-invoices' AND [IsDeleted] = 0)
            BEGIN
                DECLARE @opsGrpId INT;
                SELECT TOP 1 @opsGrpId = [Id] FROM [dbo].[Menus]
                WHERE [Name] = N'Operations' AND [ParentId] IS NULL AND [IsDeleted] = 0;
                IF @opsGrpId IS NOT NULL
                    INSERT INTO [dbo].[Menus] ([Name], [Route], [Icon], [ModuleName], [ParentId], [DisplayOrder], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                    VALUES (N'Invoice History', N'/sales-invoices', N'📋', N'Sales', @opsGrpId, 1, 1, 1, GETUTCDATE(), 0);
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
                logger.LogWarning(ex, "SaleInvoice schema batch skipped or partially applied.");
            }
        }
    }
}
