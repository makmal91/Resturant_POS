using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class InvoicePaymentInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[InvoicePayments]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[InvoicePayments] (
                    [Id]            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Module]        INT NOT NULL,
                    [SaleInvoiceId] INT NULL,
                    [PurchaseId]    INT NULL,
                    [CustomerId]    INT NULL,
                    [SupplierId]    INT NULL,
                    [PaymentType]   INT NOT NULL CONSTRAINT [DF_InvoicePayments_PaymentType] DEFAULT 1,
                    [Amount]        DECIMAL(18,2) NOT NULL,
                    [PaymentDate]   DATETIME2 NOT NULL CONSTRAINT [DF_InvoicePayments_PaymentDate] DEFAULT GETUTCDATE(),
                    [ReferenceNo]   NVARCHAR(100) NOT NULL CONSTRAINT [DF_InvoicePayments_ReferenceNo] DEFAULT N'',
                    [Notes]         NVARCHAR(500) NOT NULL CONSTRAINT [DF_InvoicePayments_Notes] DEFAULT N'',
                    [BusinessId]    INT NOT NULL CONSTRAINT [DF_InvoicePayments_BusinessId] DEFAULT 1,
                    [BranchId]      INT NOT NULL,
                    [CreatedDate]   DATETIME2 NOT NULL CONSTRAINT [DF_InvoicePayments_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById]   INT NULL,
                    [UpdatedDate]   DATETIME2 NULL,
                    [ModifiedById]  INT NULL,
                    [IsDeleted]     BIT NOT NULL CONSTRAINT [DF_InvoicePayments_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_InvoicePayments_SaleInvoices] FOREIGN KEY ([SaleInvoiceId]) REFERENCES [dbo].[SaleInvoices]([Id]),
                    CONSTRAINT [FK_InvoicePayments_Purchases] FOREIGN KEY ([PurchaseId]) REFERENCES [dbo].[Purchases]([Id]),
                    CONSTRAINT [FK_InvoicePayments_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]),
                    CONSTRAINT [FK_InvoicePayments_Suppliers] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers]([Id]),
                    CONSTRAINT [FK_InvoicePayments_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_invoicepayments_business_branch_saleinvoice' AND object_id = OBJECT_ID(N'[dbo].[InvoicePayments]'))
                CREATE INDEX [idx_invoicepayments_business_branch_saleinvoice]
                    ON [dbo].[InvoicePayments]([BusinessId], [BranchId], [SaleInvoiceId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_invoicepayments_business_branch_purchase' AND object_id = OBJECT_ID(N'[dbo].[InvoicePayments]'))
                CREATE INDEX [idx_invoicepayments_business_branch_purchase]
                    ON [dbo].[InvoicePayments]([BusinessId], [BranchId], [PurchaseId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_invoicepayments_business_branch_customer_date' AND object_id = OBJECT_ID(N'[dbo].[InvoicePayments]'))
                CREATE INDEX [idx_invoicepayments_business_branch_customer_date]
                    ON [dbo].[InvoicePayments]([BusinessId], [BranchId], [CustomerId], [PaymentDate]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_invoicepayments_business_branch_supplier_date' AND object_id = OBJECT_ID(N'[dbo].[InvoicePayments]'))
                CREATE INDEX [idx_invoicepayments_business_branch_supplier_date]
                    ON [dbo].[InvoicePayments]([BusinessId], [BranchId], [SupplierId], [PaymentDate]);
            """,
            """
            IF COL_LENGTH(N'[dbo].[InvoicePayments]', N'IsReversed') IS NULL
                ALTER TABLE [dbo].[InvoicePayments] ADD [IsReversed] BIT NOT NULL CONSTRAINT [DF_InvoicePayments_IsReversed] DEFAULT 0;
            IF COL_LENGTH(N'[dbo].[InvoicePayments]', N'OriginalPaymentId') IS NULL
                ALTER TABLE [dbo].[InvoicePayments] ADD [OriginalPaymentId] INT NULL;
            IF COL_LENGTH(N'[dbo].[InvoicePayments]', N'ReversedBy') IS NULL
                ALTER TABLE [dbo].[InvoicePayments] ADD [ReversedBy] INT NULL;
            IF COL_LENGTH(N'[dbo].[InvoicePayments]', N'ReversedAt') IS NULL
                ALTER TABLE [dbo].[InvoicePayments] ADD [ReversedAt] DATETIME2 NULL;
            IF COL_LENGTH(N'[dbo].[InvoicePayments]', N'DeletedBy') IS NULL
                ALTER TABLE [dbo].[InvoicePayments] ADD [DeletedBy] INT NULL;
            IF COL_LENGTH(N'[dbo].[InvoicePayments]', N'PaymentCategory') IS NULL
                ALTER TABLE [dbo].[InvoicePayments] ADD [PaymentCategory] NVARCHAR(20) NOT NULL
                    CONSTRAINT [DF_InvoicePayments_PaymentCategory] DEFAULT N'AgainstInvoice';
            """,
            """
            IF COL_LENGTH(N'[dbo].[SaleInvoices]', N'SettlementStatus') IS NULL
                ALTER TABLE [dbo].[SaleInvoices] ADD [SettlementStatus] INT NOT NULL CONSTRAINT [DF_SaleInvoices_SettlementStatus] DEFAULT 0;
            IF COL_LENGTH(N'[dbo].[Purchases]', N'PaidAmount') IS NULL
                ALTER TABLE [dbo].[Purchases] ADD [PaidAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Purchases_PaidAmount] DEFAULT 0;
            IF COL_LENGTH(N'[dbo].[Purchases]', N'SettlementStatus') IS NULL
                ALTER TABLE [dbo].[Purchases] ADD [SettlementStatus] INT NOT NULL CONSTRAINT [DF_Purchases_SettlementStatus] DEFAULT 0;
            """,
            """
            IF OBJECT_ID(N'[dbo].[PaymentAllocations]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PaymentAllocations] (
                    [Id]               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [InvoicePaymentId] INT NOT NULL,
                    [SaleInvoiceId]    INT NULL,
                    [PurchaseId]       INT NULL,
                    [AppliedAmount]    DECIMAL(18,2) NOT NULL,
                    [BusinessId]       INT NOT NULL CONSTRAINT [DF_PaymentAllocations_BusinessId] DEFAULT 1,
                    [BranchId]         INT NOT NULL,
                    [CreatedDate]      DATETIME2 NOT NULL CONSTRAINT [DF_PaymentAllocations_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById]      INT NULL,
                    [UpdatedDate]      DATETIME2 NULL,
                    [ModifiedById]     INT NULL,
                    [IsDeleted]        BIT NOT NULL CONSTRAINT [DF_PaymentAllocations_IsDeleted] DEFAULT 0,
                    CONSTRAINT [FK_PaymentAllocations_InvoicePayments] FOREIGN KEY ([InvoicePaymentId]) REFERENCES [dbo].[InvoicePayments]([Id]),
                    CONSTRAINT [FK_PaymentAllocations_SaleInvoices] FOREIGN KEY ([SaleInvoiceId]) REFERENCES [dbo].[SaleInvoices]([Id]),
                    CONSTRAINT [FK_PaymentAllocations_Purchases] FOREIGN KEY ([PurchaseId]) REFERENCES [dbo].[Purchases]([Id]),
                    CONSTRAINT [FK_PaymentAllocations_Branches] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_paymentallocations_business_branch_payment' AND object_id = OBJECT_ID(N'[dbo].[PaymentAllocations]'))
                CREATE INDEX [idx_paymentallocations_business_branch_payment]
                    ON [dbo].[PaymentAllocations]([BusinessId], [BranchId], [InvoicePaymentId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_paymentallocations_business_branch_saleinvoice' AND object_id = OBJECT_ID(N'[dbo].[PaymentAllocations]'))
                CREATE INDEX [idx_paymentallocations_business_branch_saleinvoice]
                    ON [dbo].[PaymentAllocations]([BusinessId], [BranchId], [SaleInvoiceId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_paymentallocations_business_branch_purchase' AND object_id = OBJECT_ID(N'[dbo].[PaymentAllocations]'))
                CREATE INDEX [idx_paymentallocations_business_branch_purchase]
                    ON [dbo].[PaymentAllocations]([BusinessId], [BranchId], [PurchaseId]);
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
                logger.LogWarning(ex, "InvoicePayment schema batch skipped or partially applied.");
            }
        }
    }
}
