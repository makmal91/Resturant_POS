using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Accounting.Interfaces;
using POSSystem.Application.Accounting.Services;
using POSSystem.Infrastructure.Repositories;

namespace POSSystem.Infrastructure.Data;

/// <summary>
/// Runtime schema patches for the parallel double-entry accounting tables (Phase 1).
/// Creates and maintains GL Accounts/Transactions; drops legacy party/cash ledger tables in Phase 10.
/// </summary>
public static class AccountingDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            // Chart of accounts
            """
            IF OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Accounts] (
                    [Id]           INT            IDENTITY(1,1) NOT NULL,
                    [Name]         NVARCHAR(200)  NOT NULL,
                    [Type]         INT            NOT NULL,
                    [ParentId]     INT            NULL,
                    [IsActive]     BIT            NOT NULL DEFAULT 1,
                    [CreatedDate]  DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedById]  INT            NULL,
                    [UpdatedDate]  DATETIME2      NULL,
                    [ModifiedById] INT            NULL,
                    [IsDeleted]    BIT            NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Accounts_Parent] FOREIGN KEY ([ParentId]) REFERENCES [Accounts]([Id])
                );
                CREATE INDEX [idx_accounts_name] ON [Accounts]([Name]) WHERE [IsDeleted] = 0;
                CREATE INDEX [idx_accounts_parentid] ON [Accounts]([ParentId]);
            END
            """,

            // General-ledger journal lines (double-entry, branch-scoped)
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Transactions] (
                    [Id]              INT            IDENTITY(1,1) NOT NULL,
                    [Date]            DATETIME2      NOT NULL,
                    [AccountId]       INT            NOT NULL,
                    [BranchId]        INT            NOT NULL,
                    [DebitAmount]     DECIMAL(18,2)  NOT NULL DEFAULT 0,
                    [CreditAmount]    DECIMAL(18,2)  NOT NULL DEFAULT 0,
                    [TransactionType] INT            NOT NULL,
                    [ReferenceId]     INT            NULL,
                    [GroupId]         UNIQUEIDENTIFIER NOT NULL,
                    [Description]     NVARCHAR(500)  NULL,
                    [CreatedAt]       DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Transactions_Accounts] FOREIGN KEY ([AccountId]) REFERENCES [Accounts]([Id]),
                    CONSTRAINT [FK_Transactions_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id])
                );
                CREATE INDEX [idx_transactions_accountid] ON [Transactions]([AccountId]);
                CREATE INDEX [idx_transactions_branchid] ON [Transactions]([BranchId]);
                CREATE INDEX [idx_transactions_date] ON [Transactions]([Date]);
                CREATE INDEX [idx_transactions_groupid] ON [Transactions]([GroupId]);
            END
            """,

            // Customer → GL receivable sub-account link
            """
            IF OBJECT_ID(N'[dbo].[Customers]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'AccountId')
                    ALTER TABLE [dbo].[Customers] ADD [AccountId] INT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Customers_Accounts')
                    ALTER TABLE [dbo].[Customers] ADD CONSTRAINT [FK_Customers_Accounts]
                        FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts]([Id]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_customer_accountid' AND object_id = OBJECT_ID(N'[dbo].[Customers]'))
                    CREATE INDEX [idx_customer_accountid] ON [dbo].[Customers]([AccountId]);
            END
            """,

            // Supplier → GL payable sub-account link
            """
            IF OBJECT_ID(N'[dbo].[Suppliers]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = N'AccountId')
                    ALTER TABLE [dbo].[Suppliers] ADD [AccountId] INT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Suppliers_Accounts')
                    ALTER TABLE [dbo].[Suppliers] ADD CONSTRAINT [FK_Suppliers_Accounts]
                        FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts]([Id]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_supplier_accountid' AND object_id = OBJECT_ID(N'[dbo].[Suppliers]'))
                    CREATE INDEX [idx_supplier_accountid] ON [dbo].[Suppliers]([AccountId]);
            END
            """,

            // Phase 6: ledger query performance
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_date_accountid' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                CREATE INDEX [idx_transactions_date_accountid] ON [dbo].[Transactions]([Date], [AccountId]);
            """,

            // Phase 7a: reversal audit trail for safe re-posting
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Transactions', N'IsReversed') IS NULL
                    ALTER TABLE [dbo].[Transactions] ADD [IsReversed] BIT NOT NULL CONSTRAINT [DF_Transactions_IsReversed] DEFAULT 0;
                IF COL_LENGTH(N'dbo.Transactions', N'ReversalOfGroupId') IS NULL
                    ALTER TABLE [dbo].[Transactions] ADD [ReversalOfGroupId] UNIQUEIDENTIFIER NULL;
                IF COL_LENGTH(N'dbo.Transactions', N'ReversedByGroupId') IS NULL
                    ALTER TABLE [dbo].[Transactions] ADD [ReversedByGroupId] UNIQUEIDENTIFIER NULL;
            END
            """,

            // Phase 7b: reference lookup index (separate batch so column adds are not blocked)
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsReversed') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_reference_type' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                CREATE INDEX [idx_transactions_reference_type] ON [dbo].[Transactions]([ReferenceId], [TransactionType], [IsReversed]);
            """,

            // Phase 7c: dual-ledger view flags (clean vs audit)
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Transactions', N'IsEdited') IS NULL
                    ALTER TABLE [dbo].[Transactions] ADD [IsEdited] BIT NOT NULL CONSTRAINT [DF_Transactions_IsEdited] DEFAULT 0;
                IF COL_LENGTH(N'dbo.Transactions', N'IsReversal') IS NULL
                    ALTER TABLE [dbo].[Transactions] ADD [IsReversal] BIT NOT NULL CONSTRAINT [DF_Transactions_IsReversal] DEFAULT 0;
                IF COL_LENGTH(N'dbo.Transactions', N'IsUpdated') IS NULL
                    ALTER TABLE [dbo].[Transactions] ADD [IsUpdated] BIT NOT NULL CONSTRAINT [DF_Transactions_IsUpdated] DEFAULT 0;
                IF COL_LENGTH(N'dbo.Transactions', N'OriginalGroupId') IS NULL
                    ALTER TABLE [dbo].[Transactions] ADD [OriginalGroupId] UNIQUEIDENTIFIER NULL;
            END
            """,

            // Phase 7d: backfill dual-ledger flags from legacy reversal data
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsEdited') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsReversal') IS NOT NULL
            BEGIN
                UPDATE [dbo].[Transactions]
                    SET [IsEdited] = [IsReversed]
                    WHERE [IsReversed] = 1 AND [TransactionType] <> 8 AND [IsEdited] = 0;
                UPDATE [dbo].[Transactions]
                    SET [IsReversal] = 1
                    WHERE [TransactionType] = 8 AND [IsReversal] = 0;
                UPDATE [dbo].[Transactions]
                    SET [OriginalGroupId] = [GroupId]
                    WHERE [OriginalGroupId] IS NULL;
            END
            """,

            // Phase 8a: single IsActive flag for reporting
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NULL
                ALTER TABLE [dbo].[Transactions] ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_Transactions_IsActive] DEFAULT 1;
            """,

            // Phase 8b: migrate legacy flags into IsActive (reversal lines)
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsReversal') IS NOT NULL
                UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsReversal] = 1;
            """,

            // Phase 8c: migrate legacy IsEdited / IsReversed into IsActive
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsEdited') IS NOT NULL
                UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsEdited] = 1;
            """,

            // Phase 8d: migrate legacy IsReversed into IsActive
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsReversed') IS NOT NULL
                UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsReversed] = 1 AND [IsReversal] = 0;
            """,

            // Phase 8e: replace reference index to use IsActive
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_reference_type' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                    DROP INDEX [idx_transactions_reference_type] ON [dbo].[Transactions];
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_reference_type' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                    CREATE INDEX [idx_transactions_reference_type] ON [dbo].[Transactions]([ReferenceId], [TransactionType], [IsActive]);
            END
            """,

            // Phase 8f: active-account index for ledger queries
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_accountid_active' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                CREATE INDEX [idx_transactions_accountid_active] ON [dbo].[Transactions]([AccountId], [IsActive]) INCLUDE ([Date], [DebitAmount], [CreditAmount]);
            """,

            // Phase 9a: drop legacy index before removing obsolete columns
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_reference_type' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
               AND (COL_LENGTH(N'dbo.Transactions', N'IsReversed') IS NOT NULL OR COL_LENGTH(N'dbo.Transactions', N'IsEdited') IS NOT NULL)
                DROP INDEX [idx_transactions_reference_type] ON [dbo].[Transactions];
            """,

            // Phase 9b: drop default constraints on legacy columns
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
            BEGIN
                DECLARE @dropDf NVARCHAR(MAX) = N'';
                SELECT @dropDf += N'ALTER TABLE [dbo].[Transactions] DROP CONSTRAINT [' + dc.name + N'];'
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Transactions]')
                  AND c.name IN (N'IsReversed', N'IsEdited', N'IsUpdated', N'ReversedByGroupId');
                IF LEN(@dropDf) > 0 EXEC sp_executesql @dropDf;
            END
            """,

            // Phase 9c: remove obsolete flag columns (IsActive replaces them)
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Transactions', N'IsReversed') IS NOT NULL
                    ALTER TABLE [dbo].[Transactions] DROP COLUMN [IsReversed];
                IF COL_LENGTH(N'dbo.Transactions', N'IsEdited') IS NOT NULL
                    ALTER TABLE [dbo].[Transactions] DROP COLUMN [IsEdited];
                IF COL_LENGTH(N'dbo.Transactions', N'IsUpdated') IS NOT NULL
                    ALTER TABLE [dbo].[Transactions] DROP COLUMN [IsUpdated];
                IF COL_LENGTH(N'dbo.Transactions', N'ReversedByGroupId') IS NOT NULL
                    ALTER TABLE [dbo].[Transactions] DROP COLUMN [ReversedByGroupId];
            END
            """,

            // Phase 9d: ensure reference index uses IsActive after cleanup
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_reference_type' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                CREATE INDEX [idx_transactions_reference_type] ON [dbo].[Transactions]([ReferenceId], [TransactionType], [IsActive]);
            """,

            // Phase 10: remove legacy party/cash ledger tables (GL Transactions is the single source of truth)
            """
            IF OBJECT_ID(N'[dbo].[CustomerLedgerTransactions]', N'U') IS NOT NULL
                DROP TABLE [dbo].[CustomerLedgerTransactions];
            """,
            """
            IF OBJECT_ID(N'[dbo].[SupplierLedgerTransactions]', N'U') IS NOT NULL
                DROP TABLE [dbo].[SupplierLedgerTransactions];
            """,
            """
            IF OBJECT_ID(N'[dbo].[CashFlowTransactions]', N'U') IS NOT NULL
                DROP TABLE [dbo].[CashFlowTransactions];
            """,

            // Phase 11a: branch-scoped transactions (COA is global; reality lives on Transactions)
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NULL
                ALTER TABLE [dbo].[Transactions] ADD [BranchId] INT NULL;
            """,

            // Phase 11b: backfill BranchId from legacy per-branch Accounts
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NOT NULL
               AND COL_LENGTH(N'dbo.Accounts', N'BranchId') IS NOT NULL
                UPDATE t SET t.[BranchId] = a.[BranchId]
                FROM [dbo].[Transactions] t
                INNER JOIN [dbo].[Accounts] a ON a.[Id] = t.[AccountId]
                WHERE t.[BranchId] IS NULL;
            """,

            // Phase 11c: default missing branch to 1
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NOT NULL
                UPDATE [dbo].[Transactions] SET [BranchId] = 1 WHERE [BranchId] IS NULL;
            """,

            // Phase 11d: enforce NOT NULL + FK on Transactions.BranchId
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Transactions_Branches')
            BEGIN
                ALTER TABLE [dbo].[Transactions] ALTER COLUMN [BranchId] INT NOT NULL;
                ALTER TABLE [dbo].[Transactions] ADD CONSTRAINT [FK_Transactions_Branches]
                    FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]);
            END
            """,

            // Phase 11e: branch query indexes
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_branchid' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                CREATE INDEX [idx_transactions_branchid] ON [dbo].[Transactions]([BranchId]);
            """,
            """
            IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Transactions', N'BranchId') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_branch_account_active' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
                CREATE INDEX [idx_transactions_branch_account_active] ON [dbo].[Transactions]([BranchId], [AccountId], [IsActive]);
            """
        };

        var batchesAfterCoaConsolidation = new[]
        {
            // Phase 11f: drop Accounts → Branches FK before removing BranchId column
            """
            IF OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NOT NULL
               AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Accounts_Branches')
                ALTER TABLE [dbo].[Accounts] DROP CONSTRAINT [FK_Accounts_Branches];
            """,

            // Phase 11g: drop legacy per-branch account indexes
            """
            IF OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_accounts_businessid' AND object_id = OBJECT_ID(N'[dbo].[Accounts]'))
                    DROP INDEX [idx_accounts_businessid] ON [dbo].[Accounts];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_accounts_branchid' AND object_id = OBJECT_ID(N'[dbo].[Accounts]'))
                    DROP INDEX [idx_accounts_branchid] ON [dbo].[Accounts];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_accounts_business_branch' AND object_id = OBJECT_ID(N'[dbo].[Accounts]'))
                    DROP INDEX [idx_accounts_business_branch] ON [dbo].[Accounts];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_accounts_business_branch_name' AND object_id = OBJECT_ID(N'[dbo].[Accounts]'))
                    DROP INDEX [idx_accounts_business_branch_name] ON [dbo].[Accounts];
            END
            """,

            // Phase 11h: remove BusinessId / BranchId from global Accounts
            """
            IF OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.Accounts', N'BusinessId') IS NOT NULL
                    ALTER TABLE [dbo].[Accounts] DROP COLUMN [BusinessId];
                IF COL_LENGTH(N'dbo.Accounts', N'BranchId') IS NOT NULL
                    ALTER TABLE [dbo].[Accounts] DROP COLUMN [BranchId];
            END
            """,

            // Phase 11i: global account name index
            """
            IF OBJECT_ID(N'[dbo].[Accounts]', N'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_accounts_name' AND object_id = OBJECT_ID(N'[dbo].[Accounts]'))
                CREATE INDEX [idx_accounts_name] ON [dbo].[Accounts]([Name]) WHERE [IsDeleted] = 0;
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
                logger.LogWarning(ex, "Accounting schema batch skipped or partially applied.");
            }
        }

        await MigrateGlobalAccountingAsync(context, logger);

        foreach (var batch in batchesAfterCoaConsolidation)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(batch);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Accounting global COA cleanup batch skipped or partially applied.");
            }
        }
    }

    /// <summary>Global COA + branch-scoped transactions migration (idempotent).</summary>
    public static async Task MigrateGlobalAccountingAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            if (!await context.Database.CanConnectAsync())
                return;

            if (!await TableExistsAsync(context, "Accounts"))
                return;

            var repository = new GlAccountRepository(context);
            await repository.MigrateToGlobalCoaAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Global accounting migration skipped or partially applied.");
        }
    }

    /// <summary>Builds professional COA hierarchy and reparents customer/supplier accounts (idempotent).</summary>
    [Obsolete("Use MigrateGlobalAccountingAsync")]
    public static Task MigrateCoaHierarchyAsync(POSDbContext context, ILogger logger) =>
        MigrateGlobalAccountingAsync(context, logger);

    /// <summary>Seeds the professional chart of accounts for every active branch (idempotent).</summary>
    public static async Task SeedDefaultAccountsAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            await MigrateGlobalAccountingAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Default GL accounts seed skipped or partially applied.");
        }
    }

    /// <summary>Creates AR/AP sub-accounts for existing customers and suppliers missing AccountId.</summary>
    public static async Task BackfillPartyAccountLinksAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            if (!await context.Database.CanConnectAsync())
                return;

            var hasCustomers = await TableHasColumnAsync(context, "Customers", "AccountId");
            var hasSuppliers = await TableHasColumnAsync(context, "Suppliers", "AccountId");
            if (!hasCustomers && !hasSuppliers)
                return;

            var service = new GlAccountService(new GlAccountRepository(context));
            await service.BackfillPartyAccountLinksAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Party GL account backfill skipped or partially applied.");
        }
    }

    /// <summary>Creates GL sub-accounts under General Expense for categories missing GlAccountId.</summary>
    public static async Task BackfillExpenseCategoryGlLinksAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            if (!await context.Database.CanConnectAsync())
                return;

            if (!await TableHasColumnAsync(context, "ExpenseCategories", "GlAccountId"))
                return;

            var service = new GlAccountService(new GlAccountRepository(context));
            await service.BackfillExpenseCategoryGlLinksAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Expense category GL account backfill skipped or partially applied.");
        }
    }

    private static async Task<bool> TableHasColumnAsync(POSDbContext context, string table, string column)
    {
        var sql = table switch
        {
            "Customers" when column == "AccountId" =>
                "SELECT CASE WHEN COL_LENGTH(N'dbo.Customers', N'AccountId') IS NULL THEN 0 ELSE 1 END",
            "Suppliers" when column == "AccountId" =>
                "SELECT CASE WHEN COL_LENGTH(N'dbo.Suppliers', N'AccountId') IS NULL THEN 0 ELSE 1 END",
            _ => "SELECT 0",
        };

        var values = await context.Database.SqlQueryRaw<int>(sql).ToListAsync();
        return values.FirstOrDefault() == 1;
    }

    private static async Task<bool> TableExistsAsync(POSDbContext context, string table)
    {
        var sql = $"SELECT CASE WHEN OBJECT_ID(N'[dbo].[{table}]', N'U') IS NULL THEN 0 ELSE 1 END";
        var values = await context.Database.SqlQueryRaw<int>(sql).ToListAsync();
        return values.FirstOrDefault() == 1;
    }
}
