-- Removes duplicate product-form opening stock GL when the same product is on an active opening stock voucher.
-- Safe to run after deploying OpeningStockDuplicateCleanupService; use for manual verification only.
--
-- Expected effect for PRD-00004 on voucher OS-00002:
--   - Product-form OpeningBalance journal reversed (inactive)
--   - Stock ledger product-form opening reversed via OpeningReversal
--   - Voucher OS-00002 entries remain as the source of truth

-- Preview products with duplicate opening (product-form + voucher)
SELECT
    p.ProductCode,
    p.ProductName,
    sle.VariantId,
    SUM(CASE WHEN sle.ReferenceId = sle.ProductId THEN sle.QuantityInBaseUnit ELSE 0 END) AS ProductFormQty,
    SUM(CASE WHEN sle.ReferenceId <> sle.ProductId THEN sle.QuantityInBaseUnit ELSE 0 END) AS VoucherFormQty
FROM StockLedgerEntries sle
INNER JOIN Products p ON p.Id = sle.ProductId
WHERE sle.IsDeleted = 0
  AND sle.Type IN (10, 11) -- Opening, OpeningReversal
GROUP BY p.ProductCode, p.ProductName, sle.ProductId, sle.VariantId, sle.BranchId
HAVING
    SUM(CASE WHEN sle.ReferenceId = sle.ProductId THEN sle.QuantityInBaseUnit ELSE 0 END) > 0
    AND SUM(CASE WHEN sle.ReferenceId <> sle.ProductId THEN sle.QuantityInBaseUnit ELSE 0 END) > 0;

-- Preview duplicate OpeningBalance GL (product-form) for products on active vouchers
SELECT DISTINCT
    p.ProductCode,
    gt.ReferenceId AS ProductId,
    gt.Description,
    gt.DebitAmount,
    gt.CreditAmount,
    gt.IsActive
FROM GlTransactions gt
INNER JOIN Products p ON p.Id = gt.ReferenceId
WHERE gt.TransactionType = 6 -- OpeningBalance
  AND gt.IsActive = 1
  AND gt.ReferenceId IN (
      SELECT DISTINCT osl.ProductId
      FROM OpeningStockVoucherLines osl
      INNER JOIN OpeningStockVouchers osv ON osv.Id = osl.VoucherId
      WHERE osl.IsDeleted = 0 AND osv.IsDeleted = 0 AND osv.IsReversed = 0
  );
