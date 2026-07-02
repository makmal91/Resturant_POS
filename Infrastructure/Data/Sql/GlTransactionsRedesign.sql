-- =============================================================================
-- GL Transactions — Clean Double-Entry Redesign
-- Restaurant POS / POSSystem
--
-- Principles:
--   • Every journal entry is a balanced set of lines sharing one GroupId.
--   • Only IsActive = 1 rows affect balances and reports.
--   • Edits never mutate posted lines — deactivate, reverse, re-post.
--   • Full audit trail preserved (inactive rows remain in the table).
--
-- Note on ReferenceId:
--   Kept as an optional operational link to source documents (Sale #, Expense #).
--   It is NOT used in balance calculations — only for lookup and duplicate prevention.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. CLEAN SCHEMA (new database or full rebuild)
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Transactions] (
        [Id]               INT              IDENTITY(1,1) NOT NULL,
        [Date]             DATETIME2        NOT NULL,
        [AccountId]        INT              NOT NULL,
        [DebitAmount]      DECIMAL(18,2)    NOT NULL CONSTRAINT [DF_Transactions_Debit] DEFAULT 0,
        [CreditAmount]     DECIMAL(18,2)    NOT NULL CONSTRAINT [DF_Transactions_Credit] DEFAULT 0,
        [TransactionType]  INT              NOT NULL,
        [GroupId]          UNIQUEIDENTIFIER NOT NULL,
        [OriginalGroupId]  UNIQUEIDENTIFIER NULL,
        [ReversalOfGroupId] UNIQUEIDENTIFIER NULL,
        [IsActive]         BIT              NOT NULL CONSTRAINT [DF_Transactions_IsActive] DEFAULT 1,
        [IsReversal]       BIT              NOT NULL CONSTRAINT [DF_Transactions_IsReversal] DEFAULT 0,
        [ReferenceId]      INT              NULL,  -- source document lookup (optional)
        [Description]      NVARCHAR(500)    NULL,
        [CreatedAt]        DATETIME2        NOT NULL CONSTRAINT [DF_Transactions_CreatedAt] DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Transactions_Accounts] FOREIGN KEY ([AccountId]) REFERENCES [dbo].[Accounts]([Id])
    );

    CREATE INDEX [idx_transactions_accountid] ON [dbo].[Transactions]([AccountId]);
    CREATE INDEX [idx_transactions_date] ON [dbo].[Transactions]([Date]);
    CREATE INDEX [idx_transactions_groupid] ON [dbo].[Transactions]([GroupId]);
    CREATE INDEX [idx_transactions_date_accountid] ON [dbo].[Transactions]([Date], [AccountId]);
    CREATE INDEX [idx_transactions_reference_type] ON [dbo].[Transactions]([ReferenceId], [TransactionType], [IsActive]);
    CREATE INDEX [idx_transactions_accountid_active] ON [dbo].[Transactions]([AccountId], [IsActive])
        INCLUDE ([Date], [DebitAmount], [CreditAmount]);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. UPGRADE EXISTING DATABASE (from legacy IsReversed / IsEdited flags)
-- -----------------------------------------------------------------------------

IF COL_LENGTH(N'dbo.Transactions', N'IsActive') IS NULL
    ALTER TABLE [dbo].[Transactions] ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_Transactions_IsActive] DEFAULT 1;
GO

IF COL_LENGTH(N'dbo.Transactions', N'IsReversal') IS NOT NULL
    UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsReversal] = 1;
GO

IF COL_LENGTH(N'dbo.Transactions', N'IsEdited') IS NOT NULL
    UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsEdited] = 1;
GO

IF COL_LENGTH(N'dbo.Transactions', N'IsReversed') IS NOT NULL
    UPDATE [dbo].[Transactions] SET [IsActive] = 0 WHERE [IsReversed] = 1 AND [IsReversal] = 0;
GO

IF COL_LENGTH(N'dbo.Transactions', N'OriginalGroupId') IS NOT NULL
    UPDATE [dbo].[Transactions] SET [OriginalGroupId] = [GroupId] WHERE [OriginalGroupId] IS NULL;
GO

-- Optional: drop legacy columns after verifying data (run manually when ready)
-- ALTER TABLE [dbo].[Transactions] DROP CONSTRAINT [DF_Transactions_IsReversed];
-- ALTER TABLE [dbo].[Transactions] DROP COLUMN [IsReversed], [IsEdited], [IsUpdated], [ReversedByGroupId];

-- -----------------------------------------------------------------------------
-- 3. SAMPLE INSERT — $500 expense paid from cash (double entry)
-- TransactionType: 5 = Expense
-- -----------------------------------------------------------------------------

DECLARE @GroupId UNIQUEIDENTIFIER = NEWID();
DECLARE @ExpenseAccountId INT = (SELECT TOP 1 Id FROM Accounts WHERE Name = N'General Expense' AND IsDeleted = 0);
DECLARE @CashAccountId INT = (SELECT TOP 1 Id FROM Accounts WHERE Name = N'Cash' AND IsDeleted = 0);
DECLARE @ExpenseId INT = 42;  -- source Expense.Id

INSERT INTO [dbo].[Transactions]
    ([Date], [AccountId], [DebitAmount], [CreditAmount], [TransactionType], [GroupId], [OriginalGroupId], [IsActive], [IsReversal], [ReferenceId], [Description])
VALUES
    (GETUTCDATE(), @ExpenseAccountId, 500.00, 0.00, 5, @GroupId, @GroupId, 1, 0, @ExpenseId, N'Expense — Office supplies'),
    (GETUTCDATE(), @CashAccountId, 0.00, 500.00, 5, @GroupId, @GroupId, 1, 0, @ExpenseId, N'Expense — Office supplies');

-- Validation: must balance
-- SELECT GroupId, SUM(DebitAmount) AS TotalDebit, SUM(CreditAmount) AS TotalCredit
-- FROM Transactions WHERE GroupId = @GroupId GROUP BY GroupId;

-- -----------------------------------------------------------------------------
-- 4. EDIT TRANSACTION LOGIC (reverse + re-post)
-- -----------------------------------------------------------------------------

/*
PSEUDOCODE — EditExpense(expenseId, newAmount):

  -- Step 1: find active journal for this expense
  activeLines = SELECT * FROM Transactions
                WHERE ReferenceId = @expenseId AND TransactionType = 5 AND IsActive = 1

  IF activeLines is empty → error "nothing to edit"

  @oldGroupId = activeLines[0].GroupId
  @chainId     = COALESCE(activeLines[0].OriginalGroupId, @oldGroupId)
  @reversalId  = NEWID()
  @newGroupId  = NEWID()

  BEGIN TRANSACTION

  -- Step 1: deactivate originals
  UPDATE Transactions SET IsActive = 0
  WHERE ReferenceId = @expenseId AND TransactionType = 5 AND IsActive = 1

  -- Step 2: post inactive reversal (swap debit/credit)
  INSERT reversal lines for each activeLine:
    Debit  = activeLine.Credit
    Credit = activeLine.Debit
    GroupId = @reversalId
    IsActive = 0, IsReversal = 1
    ReversalOfGroupId = activeLine.GroupId
    OriginalGroupId = @chainId
    TransactionType = 8  -- Reversal

  -- Step 3: post new active journal with updated amounts
  INSERT new lines:
    GroupId = @newGroupId
    OriginalGroupId = @chainId
    IsActive = 1, IsReversal = 0
    ReferenceId = @expenseId, TransactionType = 5
    amounts = new values

  COMMIT

RESULT:
  • Expense account shows ONLY the new $amount (active lines)
  • Old version + reversal preserved for audit (IsActive = 0)
  • Net effect on expense account = new amount (not doubled)
*/

-- -----------------------------------------------------------------------------
-- 5. EXAMPLE REPORT QUERIES
-- -----------------------------------------------------------------------------

-- Account balance (active lines only)
SELECT
    a.[Name],
    SUM(t.[DebitAmount] - t.[CreditAmount]) AS SignedBalance
FROM [dbo].[Transactions] t
INNER JOIN [dbo].[Accounts] a ON a.[Id] = t.[AccountId]
WHERE t.[IsActive] = 1
  AND t.[AccountId] = @AccountId
GROUP BY a.[Name];

-- Period expense total (General Expense account, active only)
SELECT SUM(t.[DebitAmount] - t.[CreditAmount]) AS ExpenseTotal
FROM [dbo].[Transactions] t
INNER JOIN [dbo].[Accounts] a ON a.[Id] = t.[AccountId]
WHERE t.[IsActive] = 1
  AND a.[Name] = N'General Expense'
  AND t.[Date] >= @FromDate
  AND t.[Date] < DATEADD(DAY, 1, @ToDate);

-- Account ledger with running balance (active only)
;WITH Movements AS (
    SELECT
        t.[Id], t.[Date], t.[Description],
        t.[DebitAmount], t.[CreditAmount],
        SUM(t.[DebitAmount] - t.[CreditAmount]) OVER (ORDER BY t.[Date], t.[Id]) AS RunningBalance
    FROM [dbo].[Transactions] t
    WHERE t.[AccountId] = @AccountId
      AND t.[IsActive] = 1
)
SELECT * FROM Movements ORDER BY [Date], [Id];

-- Audit view: full chain for one business event
SELECT *
FROM [dbo].[Transactions]
WHERE [OriginalGroupId] = @ChainId OR [GroupId] = @ChainId
ORDER BY [Date], [Id];

-- Journal balance check (every GroupId must balance)
SELECT [GroupId],
       SUM([DebitAmount]) AS TotalDebit,
       SUM([CreditAmount]) AS TotalCredit,
       SUM([DebitAmount]) - SUM([CreditAmount]) AS Imbalance
FROM [dbo].[Transactions]
GROUP BY [GroupId]
HAVING SUM([DebitAmount]) <> SUM([CreditAmount]);

-- Only one active journal per source document (duplicate detection)
SELECT [ReferenceId], [TransactionType], COUNT(DISTINCT [GroupId]) AS ActiveGroupCount
FROM [dbo].[Transactions]
WHERE [IsActive] = 1 AND [ReferenceId] IS NOT NULL AND [TransactionType] <> 8
GROUP BY [ReferenceId], [TransactionType]
HAVING COUNT(DISTINCT [GroupId]) > 1;

-- -----------------------------------------------------------------------------
-- 6. PREVENT DUPLICATE ENTRIES
-- -----------------------------------------------------------------------------

/*
Application layer (implemented in AccountingService / AccountingIntegrationService):
  1. ExistsForReferenceAsync — skip post if active journal already exists
  2. ValidateEntries — require balanced debit/credit per GroupId
  3. Serializable transaction wrapper for edit flows (RunInTransactionAsync)
  4. Reverse-then-repost pattern (never UPDATE posted amounts)

Optional database constraint (filtered unique index — one active group per document):
*/
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Transactions_ActiveReference'
      AND object_id = OBJECT_ID(N'dbo.Transactions'))
BEGIN
    CREATE UNIQUE INDEX [UX_Transactions_ActiveReference]
    ON [dbo].[Transactions]([ReferenceId], [TransactionType])
    WHERE [IsActive] = 1 AND [ReferenceId] IS NOT NULL AND [TransactionType] <> 8;
END;
GO
