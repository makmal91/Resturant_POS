using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class CashFlowDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            // CashFlowTransactions table
            """
            IF OBJECT_ID(N'[dbo].[CashFlowTransactions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CashFlowTransactions] (
                    [Id]              INT            IDENTITY(1,1) NOT NULL,
                    [BusinessId]      INT            NOT NULL DEFAULT 1,
                    [BranchId]        INT            NOT NULL DEFAULT 1,
                    [TransactionType] INT            NOT NULL,
                    [PaymentMethod]   INT            NOT NULL DEFAULT 1,
                    [Amount]          DECIMAL(18,2)  NOT NULL DEFAULT 0,
                    [ReferenceId]     INT            NULL,
                    [ReferenceNo]     NVARCHAR(100)  NULL,
                    [Description]     NVARCHAR(500)  NULL,
                    [TransactionDate] DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedDate]     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedById]     INT            NULL,
                    [UpdatedDate]     DATETIME2      NULL,
                    [ModifiedById]    INT            NULL,
                    [IsDeleted]       BIT            NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_CashFlowTransactions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CashFlowTransactions_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id])
                );
                CREATE INDEX [idx_cashflowtransactions_businessid] ON [CashFlowTransactions]([BusinessId]);
                CREATE INDEX [idx_cashflowtransactions_branchid]   ON [CashFlowTransactions]([BranchId]);
                CREATE INDEX [idx_cashflowtransactions_business_branch] ON [CashFlowTransactions]([BusinessId],[BranchId]);
                CREATE INDEX [idx_cashflowtransactions_date]  ON [CashFlowTransactions]([BusinessId],[BranchId],[TransactionDate]);
                CREATE INDEX [idx_cashflowtransactions_type]  ON [CashFlowTransactions]([BusinessId],[BranchId],[TransactionType]);
            END
            """,

            // CashRegisters table
            """
            IF OBJECT_ID(N'[dbo].[CashRegisters]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CashRegisters] (
                    [Id]           INT           IDENTITY(1,1) NOT NULL,
                    [BusinessId]   INT           NOT NULL DEFAULT 1,
                    [BranchId]     INT           NOT NULL DEFAULT 1,
                    [RegisterDate] DATE          NOT NULL,
                    [OpeningCash]  DECIMAL(18,2) NOT NULL DEFAULT 0,
                    [ClosingCash]  DECIMAL(18,2) NULL,
                    [ExpectedCash] DECIMAL(18,2) NULL,
                    [ActualCash]   DECIMAL(18,2) NULL,
                    [Difference]   DECIMAL(18,2) NULL,
                    [IsClosed]     BIT           NOT NULL DEFAULT 0,
                    [Notes]        NVARCHAR(500) NULL,
                    [ClosedBy]     INT           NULL,
                    [ClosedAt]     DATETIME2     NULL,
                    [CreatedDate]  DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedById]  INT           NULL,
                    [UpdatedDate]  DATETIME2     NULL,
                    [ModifiedById] INT           NULL,
                    [IsDeleted]    BIT           NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_CashRegisters] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CashRegisters_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id])
                );
                CREATE INDEX [idx_cashregisters_businessid] ON [CashRegisters]([BusinessId]);
                CREATE INDEX [idx_cashregisters_branchid]   ON [CashRegisters]([BranchId]);
                CREATE UNIQUE INDEX [uq_cashregisters_branch_date]
                    ON [CashRegisters]([BusinessId],[BranchId],[RegisterDate])
                    WHERE [IsDeleted] = 0;
            END
            """,

            // Expenses table (ExpenseCategoryId populated by MasterDataDatabaseInitializer)
            """
            IF OBJECT_ID(N'[dbo].[Expenses]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Expenses] (
                    [Id]                INT           IDENTITY(1,1) NOT NULL,
                    [BusinessId]        INT           NOT NULL DEFAULT 1,
                    [BranchId]          INT           NOT NULL DEFAULT 1,
                    [ExpenseCategoryId] INT           NULL,
                    [Description]       NVARCHAR(500) NOT NULL,
                    [Amount]            DECIMAL(18,2) NOT NULL DEFAULT 0,
                    [PaymentMethod]     INT           NOT NULL DEFAULT 1,
                    [ExpenseDate]       DATE          NOT NULL DEFAULT GETUTCDATE(),
                    [ReferenceNo]       NVARCHAR(100) NULL,
                    [Notes]             NVARCHAR(500) NULL,
                    [CreatedDate]       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedById]       INT           NULL,
                    [UpdatedDate]       DATETIME2     NULL,
                    [ModifiedById]      INT           NULL,
                    [IsDeleted]         BIT           NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Expenses_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id])
                );
                CREATE INDEX [idx_expenses_businessid] ON [Expenses]([BusinessId]);
                CREATE INDEX [idx_expenses_branchid]   ON [Expenses]([BranchId]);
                CREATE INDEX [idx_expenses_business_branch] ON [Expenses]([BusinessId],[BranchId]);
                CREATE INDEX [idx_expenses_date] ON [Expenses]([BusinessId],[BranchId],[ExpenseDate]);
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
                logger.LogWarning(ex, "CashFlow schema batch skipped or partially applied.");
            }
        }
    }
}
