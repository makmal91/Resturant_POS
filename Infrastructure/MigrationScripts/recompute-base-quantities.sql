/*
  Recompute base-unit quantities for the SMALLEST-unit-as-base convention.

  Convention: ConversionFactor = number of BASE units contained in 1 of this unit
  (base unit = 1). So base quantity = entered quantity × factor.
  Older rows were stored with the inverted (quantity ÷ factor) formula.

  This script is IDEMPOTENT: it recomputes from the source columns
  (UnitQuantity / Quantity × factor), so it is safe to run more than once.
  Only run it once after deploying the fixed code; new rows are already correct.
*/
SET NOCOUNT ON;

-- 1) Stock ledger: base qty = |UnitQuantity| × unit factor, preserving in/out direction
--    (SaleEntry is negative, PurchaseEntry/Reversal positive, etc.).
UPDATE sl
SET sl.[QuantityInBaseUnit] =
        CASE WHEN sl.[QuantityInBaseUnit] < 0 THEN -1 ELSE 1 END
        * ABS(sl.[UnitQuantity]) * pu.[ConversionFactor]
FROM [dbo].[StockLedger] sl
INNER JOIN [dbo].[ProductUnits] pu ON pu.[Id] = sl.[UnitId]
WHERE sl.[UnitId] IS NOT NULL
  AND sl.[UnitQuantity] IS NOT NULL
  AND sl.[UnitQuantity] <> 0
  AND sl.[QuantityInBaseUnit] <> 0
  AND pu.[ConversionFactor] > 0;

-- 2) Purchase items: base qty = Quantity × factor.
UPDATE [dbo].[PurchaseItems]
SET [BaseQuantity] = [Quantity] * [ConversionFactor]
WHERE [ConversionFactor] > 0;

-- 3) Sale invoice items: base qty = Quantity × factor (drives COGS and unit-wise reports).
UPDATE [dbo].[SaleInvoiceItems]
SET [BaseQuantity] = [Quantity] * [ConversionFactor]
WHERE [ConversionFactor] > 0;

SELECT
    (SELECT COUNT(*) FROM [dbo].[StockLedger] WHERE [UnitId] IS NOT NULL) AS StockLedgerRows,
    (SELECT COUNT(*) FROM [dbo].[PurchaseItems])   AS PurchaseItemRows,
    (SELECT COUNT(*) FROM [dbo].[SaleInvoiceItems]) AS SaleItemRows;
GO
