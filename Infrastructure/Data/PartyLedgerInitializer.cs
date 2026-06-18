using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class PartyLedgerInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleInvoices]') AND name = N'IsCreditSale')
                ALTER TABLE [dbo].[SaleInvoices]
                    ADD [IsCreditSale] BIT NOT NULL CONSTRAINT [DF_SaleInvoices_IsCreditSale] DEFAULT 0;
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Purchases]') AND name = N'IsCreditPurchase')
                ALTER TABLE [dbo].[Purchases]
                    ADD [IsCreditPurchase] BIT NOT NULL CONSTRAINT [DF_Purchases_IsCreditPurchase] DEFAULT 0;
            """,
            """
            IF OBJECT_ID(N'[dbo].[CustomerLedgerTransactions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CustomerLedgerTransactions] (
                    [Id]             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CustomerId]     INT NOT NULL,
                    [ReferenceId]    INT NOT NULL,
                    [Type]           INT NOT NULL,
                    [Debit]          DECIMAL(18,2) NOT NULL CONSTRAINT [DF_CustomerLedger_Debit] DEFAULT 0,
                    [Credit]         DECIMAL(18,2) NOT NULL CONSTRAINT [DF_CustomerLedger_Credit] DEFAULT 0,
                    [Date]           DATETIME2 NOT NULL CONSTRAINT [DF_CustomerLedger_Date] DEFAULT GETUTCDATE(),
                    [RunningBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_CustomerLedger_RunningBalance] DEFAULT 0,
                    [Remarks]        NVARCHAR(500) NOT NULL CONSTRAINT [DF_CustomerLedger_Remarks] DEFAULT N'',
                    [BusinessId]     INT NOT NULL CONSTRAINT [DF_CustomerLedger_BusinessId] DEFAULT 1,
                    [BranchId]       INT NOT NULL,
                    [CreatedDate]    DATETIME2 NOT NULL CONSTRAINT [DF_CustomerLedger_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById]    INT NULL,
                    [UpdatedDate]    DATETIME2 NULL,
                    [ModifiedById]   INT NULL,
                    [IsDeleted]      BIT NOT NULL CONSTRAINT [DF_CustomerLedger_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_CustomerLedger_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]),
                    CONSTRAINT [FK_CustomerLedger_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customerledger_business_branch_customer_date' AND object_id = OBJECT_ID(N'[dbo].[CustomerLedgerTransactions]'))
                CREATE INDEX [idx_customerledger_business_branch_customer_date]
                    ON [dbo].[CustomerLedgerTransactions]([BusinessId], [BranchId], [CustomerId], [Date]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customerledger_business_branch_reference_type' AND object_id = OBJECT_ID(N'[dbo].[CustomerLedgerTransactions]'))
                CREATE INDEX [idx_customerledger_business_branch_reference_type]
                    ON [dbo].[CustomerLedgerTransactions]([BusinessId], [BranchId], [ReferenceId], [Type]);
            """,
            """
            IF OBJECT_ID(N'[dbo].[SupplierLedgerTransactions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SupplierLedgerTransactions] (
                    [Id]             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SupplierId]     INT NOT NULL,
                    [ReferenceId]    INT NOT NULL,
                    [Type]           INT NOT NULL,
                    [Debit]          DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SupplierLedger_Debit] DEFAULT 0,
                    [Credit]         DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SupplierLedger_Credit] DEFAULT 0,
                    [Date]           DATETIME2 NOT NULL CONSTRAINT [DF_SupplierLedger_Date] DEFAULT GETUTCDATE(),
                    [RunningBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_SupplierLedger_RunningBalance] DEFAULT 0,
                    [Remarks]        NVARCHAR(500) NOT NULL CONSTRAINT [DF_SupplierLedger_Remarks] DEFAULT N'',
                    [BusinessId]     INT NOT NULL CONSTRAINT [DF_SupplierLedger_BusinessId] DEFAULT 1,
                    [BranchId]       INT NOT NULL,
                    [CreatedDate]    DATETIME2 NOT NULL CONSTRAINT [DF_SupplierLedger_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById]    INT NULL,
                    [UpdatedDate]    DATETIME2 NULL,
                    [ModifiedById]   INT NULL,
                    [IsDeleted]      BIT NOT NULL CONSTRAINT [DF_SupplierLedger_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_SupplierLedger_Suppliers] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers]([Id]),
                    CONSTRAINT [FK_SupplierLedger_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_supplierledger_business_branch_supplier_date' AND object_id = OBJECT_ID(N'[dbo].[SupplierLedgerTransactions]'))
                CREATE INDEX [idx_supplierledger_business_branch_supplier_date]
                    ON [dbo].[SupplierLedgerTransactions]([BusinessId], [BranchId], [SupplierId], [Date]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_supplierledger_business_branch_reference_type' AND object_id = OBJECT_ID(N'[dbo].[SupplierLedgerTransactions]'))
                CREATE INDEX [idx_supplierledger_business_branch_reference_type]
                    ON [dbo].[SupplierLedgerTransactions]([BusinessId], [BranchId], [ReferenceId], [Type]);
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[PermissionModules] WHERE [ModuleName] = N'Party Ledger' AND [IsDeleted] = 0)
                INSERT INTO [dbo].[PermissionModules] ([ModuleName], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                VALUES (N'Party Ledger', 1, 1, GETUTCDATE(), 0);
            """,
            SeedPartyLedgerMenusSql()
        };

        foreach (var batch in batches)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(batch);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "PartyLedger schema batch skipped or partially applied.");
            }
        }
    }

    private static string SeedPartyLedgerMenusSql() => """
        DECLARE @accountsGroupId INT;
        SELECT TOP 1 @accountsGroupId = [Id] FROM [dbo].[Menus]
        WHERE [Name] = N'Accounts' AND [ParentId] IS NULL AND [IsDeleted] = 0;

        IF @accountsGroupId IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Route] = N'/ledger/customers' AND [Name] = N'Receive Payment' AND [IsDeleted] = 0)
                INSERT INTO [dbo].[Menus] ([Name], [Route], [Icon], [ModuleName], [ParentId], [DisplayOrder], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                VALUES (N'Receive Payment', N'/ledger/customers', N'RP', N'Party Ledger', @accountsGroupId, 6, 1, 1, GETUTCDATE(), 0);

            IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Route] = N'/ledger/suppliers' AND [Name] = N'Pay Supplier' AND [IsDeleted] = 0)
                INSERT INTO [dbo].[Menus] ([Name], [Route], [Icon], [ModuleName], [ParentId], [DisplayOrder], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                VALUES (N'Pay Supplier', N'/ledger/suppliers', N'PS', N'Party Ledger', @accountsGroupId, 7, 1, 1, GETUTCDATE(), 0);

            UPDATE [dbo].[Menus] SET [Route] = N'/ledger/customers'
                WHERE [Route] = N'/ledger/customer-payment' AND [IsDeleted] = 0;
            UPDATE [dbo].[Menus] SET [Route] = N'/ledger/suppliers'
                WHERE [Route] = N'/ledger/supplier-payment' AND [IsDeleted] = 0;

            IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Route] = N'/ledger/customers' AND [Name] = N'Customer Ledger' AND [IsDeleted] = 0)
                INSERT INTO [dbo].[Menus] ([Name], [Route], [Icon], [ModuleName], [ParentId], [DisplayOrder], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                VALUES (N'Customer Ledger', N'/ledger/customers', N'CL', N'Party Ledger', @accountsGroupId, 8, 1, 1, GETUTCDATE(), 0);

            IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Route] = N'/ledger/suppliers' AND [IsDeleted] = 0)
                INSERT INTO [dbo].[Menus] ([Name], [Route], [Icon], [ModuleName], [ParentId], [DisplayOrder], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                VALUES (N'Supplier Ledger', N'/ledger/suppliers', N'SL', N'Party Ledger', @accountsGroupId, 9, 1, 1, GETUTCDATE(), 0);
        END
        """;
}
