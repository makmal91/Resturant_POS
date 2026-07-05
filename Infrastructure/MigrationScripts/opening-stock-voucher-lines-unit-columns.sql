-- Upgrade OpeningStockVoucherLines for variant + multi-unit support.
-- Run with sqlcmd (GO separates batches — required for SQL Server compile order).

IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'VariantId') IS NULL
    ALTER TABLE [dbo].[OpeningStockVoucherLines] ADD [VariantId] INT NULL;
GO

IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'UnitId') IS NULL
    ALTER TABLE [dbo].[OpeningStockVoucherLines] ADD [UnitId] INT NULL;
GO

IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'UnitQuantity') IS NULL
    ALTER TABLE [dbo].[OpeningStockVoucherLines] ADD [UnitQuantity] DECIMAL(18,4) NOT NULL
        CONSTRAINT [DF_OpeningStockVoucherLines_UnitQuantity_Legacy] DEFAULT 0;
GO

IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'ConversionFactor') IS NULL
    ALTER TABLE [dbo].[OpeningStockVoucherLines] ADD [ConversionFactor] DECIMAL(18,4) NOT NULL
        CONSTRAINT [DF_OpeningStockVoucherLines_ConversionFactor_Legacy] DEFAULT 1;
GO

IF OBJECT_ID(N'[dbo].[OpeningStockVoucherLines]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'UnitQuantity') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[OpeningStockVoucherLines]', N'Quantity') IS NOT NULL
    UPDATE [dbo].[OpeningStockVoucherLines]
    SET [UnitQuantity] = [Quantity], [ConversionFactor] = 1
    WHERE [UnitQuantity] = 0 AND [Quantity] <> 0;
GO
