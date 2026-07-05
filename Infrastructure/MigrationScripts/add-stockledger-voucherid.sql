-- Adds StockLedger.VoucherId for opening-stock voucher linking.
IF OBJECT_ID(N'[dbo].[StockLedger]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[StockLedger]', N'VoucherId') IS NULL
    ALTER TABLE [dbo].[StockLedger] ADD [VoucherId] INT NULL;

IF OBJECT_ID(N'[dbo].[StockLedger]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[StockLedger]', N'VoucherId') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'idx_ledger_voucher_type'
          AND object_id = OBJECT_ID(N'[dbo].[StockLedger]'))
    CREATE INDEX [idx_ledger_voucher_type] ON [dbo].[StockLedger]([VoucherId], [Type]);
