-- =============================================================================
-- Ledger cleanup (incremental, non-breaking)
-- Run after API backup. Idempotent — safe to re-run.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- STEP 1: Legacy party/cash tables — DO NOT DROP (still in use)
-- -----------------------------------------------------------------------------
/*
  BLOCKED — active code dependencies:

  CustomerLedgerTransactions
    - PartyLedgerRepository (legacy writes: RecordCreditSaleAsync)
    - PartyLedgerService write paths

  SupplierLedgerTransactions
    - PartyLedgerRepository.GetSupplierLedgerPagedAsync / unified supplier ledger
    - Supplier ledger UI reads this path today

  CashFlowTransactions
    - CashFlowService (register open/close, manual cash in/out)
    - DashboardController cash summaries
    - CashFlowRepository reconciliation

  When supplier/customer ledgers are fully GL-only and cash register migrates,
  re-run guarded DROP scripts below.
*/

-- Guarded drop (leave commented until migration complete):
/*
IF OBJECT_ID(N'[dbo].[CustomerLedgerTransactions]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID(N'[dbo].[CustomerLedgerTransactions]'))
    DROP TABLE [dbo].[CustomerLedgerTransactions];
GO
IF OBJECT_ID(N'[dbo].[SupplierLedgerTransactions]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID(N'[dbo].[SupplierLedgerTransactions]'))
    DROP TABLE [dbo].[SupplierLedgerTransactions];
GO
*/

-- -----------------------------------------------------------------------------
-- STEP 2 & 3: Transactions table — ensure IsActive, remove legacy flags
-- -----------------------------------------------------------------------------

IF COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NULL
    ALTER TABLE [dbo].[Transactions] ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_Transactions_IsActive] DEFAULT 1;
GO

-- Backfill IsActive from legacy flags before dropping them
IF COL_LENGTH(N'dbo.Transactions', N'IsReversal') IS NOT NULL
    UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsReversal] = 1;
GO
IF COL_LENGTH(N'dbo.Transactions', N'IsEdited') IS NOT NULL
    UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsEdited] = 1;
GO
IF COL_LENGTH(N'dbo.Transactions', N'IsReversed') IS NOT NULL
    UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsReversed] = 1 AND ([IsReversal] = 0 OR COL_LENGTH(N'dbo.Transactions', N'IsReversal') IS NULL);
GO

-- Drop legacy index that references IsReversed
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_reference_type' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
    DROP INDEX [idx_transactions_reference_type] ON [dbo].[Transactions];
GO

-- Drop default constraints then legacy columns
DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += N'ALTER TABLE [dbo].[Transactions] DROP CONSTRAINT [' + dc.name + N'];' + CHAR(10)
FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Transactions]')
  AND c.name IN (N'IsReversed', N'IsEdited', N'IsUpdated', N'ReversedByGroupId');

IF LEN(@sql) > 0 EXEC sp_executesql @sql;
GO

IF COL_LENGTH(N'dbo.Transactions', N'IsReversed') IS NOT NULL
    ALTER TABLE [dbo].[Transactions] DROP COLUMN [IsReversed];
GO
IF COL_LENGTH(N'dbo.Transactions', N'IsEdited') IS NOT NULL
    ALTER TABLE [dbo].[Transactions] DROP COLUMN [IsEdited];
GO
IF COL_LENGTH(N'dbo.Transactions', N'IsUpdated') IS NOT NULL
    ALTER TABLE [dbo].[Transactions] DROP COLUMN [IsUpdated];
GO
IF COL_LENGTH(N'dbo.Transactions', N'ReversedByGroupId') IS NOT NULL
    ALTER TABLE [dbo].[Transactions] DROP COLUMN [ReversedByGroupId];
GO

-- ReferenceId: RETAINED (required for reverse/idempotency in AccountingRepository).
-- Do not drop unless a replacement lookup table is added first.

-- Recreate active reference index
IF COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_reference_type' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
    CREATE INDEX [idx_transactions_reference_type] ON [dbo].[Transactions]([ReferenceId], [TransactionType], [IsActive]);
GO

IF COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_transactions_accountid_active' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
    CREATE INDEX [idx_transactions_accountid_active] ON [dbo].[Transactions]([AccountId], [IsActive])
        INCLUDE ([Date], [DebitAmount], [CreditAmount]);
GO

-- Optional: one active journal per source document (fails if duplicates already exist)
/*
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Transactions_ActiveReference' AND object_id = OBJECT_ID(N'[dbo].[Transactions]'))
    CREATE UNIQUE INDEX [UX_Transactions_ActiveReference]
    ON [dbo].[Transactions]([ReferenceId], [TransactionType])
    WHERE [IsActive] = 1 AND [ReferenceId] IS NOT NULL AND [TransactionType] <> 8;
GO
*/

-- -----------------------------------------------------------------------------
-- STEP 6: Sample ledger query (active lines only)
-- -----------------------------------------------------------------------------
/*
SELECT t.Id, t.Date, t.Description, t.DebitAmount, t.CreditAmount,
       SUM(t.DebitAmount - t.CreditAmount) OVER (ORDER BY t.Date, t.Id) AS RunningBalance
FROM [dbo].[Transactions] t
WHERE t.AccountId = @AccountId
  AND t.IsActive = 1
ORDER BY t.Date, t.Id;
*/
