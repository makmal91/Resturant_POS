-- Multi-unit inventory refactor (ALTER only — safe to re-run)
-- Stock is stored ONLY in base units via StockLedger.QuantityInBaseUnit.
-- ConversionFactor = number of base units in 1 unit of the given measure.

SET NOCOUNT ON;

-- ─── ProductUnits: link to Unit Master ───────────────────────────────────────
IF COL_LENGTH(N'dbo.ProductUnits', N'UnitId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductUnits] ADD [UnitId] INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ProductUnits_Units_UnitId'
      AND parent_object_id = OBJECT_ID(N'dbo.ProductUnits')
)
BEGIN
    ALTER TABLE [dbo].[ProductUnits]
        ADD CONSTRAINT [FK_ProductUnits_Units_UnitId]
        FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'idx_productunit_unit_id'
      AND object_id = OBJECT_ID(N'dbo.ProductUnits')
)
BEGIN
    CREATE INDEX [idx_productunit_unit_id] ON [dbo].[ProductUnits]([UnitId]);
END
GO

-- Backfill UnitId from UnitName → Units.Name (same branch)
UPDATE pu
SET pu.[UnitId] = u.[Id]
FROM [dbo].[ProductUnits] pu
INNER JOIN [dbo].[Units] u
    ON u.[BusinessId] = pu.[BusinessId]
   AND u.[BranchId] = pu.[BranchId]
   AND LTRIM(RTRIM(u.[Name])) = LTRIM(RTRIM(pu.[UnitName]))
   AND u.[IsDeleted] = 0
WHERE pu.[UnitId] IS NULL
  AND pu.[IsDeleted] = 0;
GO

-- ─── Products: denormalized BaseUnitId (FK → ProductUnits) ───────────────────
IF COL_LENGTH(N'dbo.Products', N'BaseUnitId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [BaseUnitId] INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Products_ProductUnits_BaseUnitId'
      AND parent_object_id = OBJECT_ID(N'dbo.Products')
)
BEGIN
    ALTER TABLE [dbo].[Products]
        ADD CONSTRAINT [FK_Products_ProductUnits_BaseUnitId]
        FOREIGN KEY ([BaseUnitId]) REFERENCES [dbo].[ProductUnits]([Id]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'idx_product_base_unit_id'
      AND object_id = OBJECT_ID(N'dbo.Products')
)
BEGIN
    CREATE INDEX [idx_product_base_unit_id] ON [dbo].[Products]([BaseUnitId]);
END
GO

-- Backfill BaseUnitId from IsBaseUnit flag
UPDATE p
SET p.[BaseUnitId] = bu.[Id]
FROM [dbo].[Products] p
INNER JOIN [dbo].[ProductUnits] bu
    ON bu.[ProductId] = p.[Id]
   AND bu.[IsBaseUnit] = 1
   AND bu.[IsDeleted] = 0
WHERE p.[BaseUnitId] IS NULL
  AND p.[IsDeleted] = 0;
GO

-- Enforce base unit ConversionFactor = 1 (base unit is always 1:1 with stock)
UPDATE [dbo].[ProductUnits]
SET [ConversionFactor] = 1
WHERE [IsBaseUnit] = 1
  AND [IsDeleted] = 0
  AND [ConversionFactor] <> 1;
GO

-- ─── StockLedger: audit columns for entered unit/qty ─────────────────────────
IF COL_LENGTH(N'dbo.StockLedger', N'UnitId') IS NULL
BEGIN
    ALTER TABLE [dbo].[StockLedger] ADD [UnitId] INT NULL;
END
GO

IF COL_LENGTH(N'dbo.StockLedger', N'UnitQuantity') IS NULL
BEGIN
    ALTER TABLE [dbo].[StockLedger] ADD [UnitQuantity] DECIMAL(18,4) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_StockLedger_ProductUnits_UnitId'
      AND parent_object_id = OBJECT_ID(N'dbo.StockLedger')
)
BEGIN
    ALTER TABLE [dbo].[StockLedger]
        ADD CONSTRAINT [FK_StockLedger_ProductUnits_UnitId]
        FOREIGN KEY ([UnitId]) REFERENCES [dbo].[ProductUnits]([Id]);
END
GO

-- Backfill BaseQuantity on line items where missing (legacy rows)
UPDATE pi
SET pi.[BaseQuantity] = pi.[Quantity] * pi.[ConversionFactor]
FROM [dbo].[PurchaseItems] pi
WHERE pi.[IsDeleted] = 0
  AND pi.[BaseQuantity] = 0
  AND pi.[Quantity] <> 0
  AND pi.[ConversionFactor] > 0;
GO

UPDATE si
SET si.[BaseQuantity] = si.[Quantity] * si.[ConversionFactor]
FROM [dbo].[SaleInvoiceItems] si
WHERE si.[IsDeleted] = 0
  AND si.[BaseQuantity] = 0
  AND si.[Quantity] <> 0
  AND si.[ConversionFactor] > 0;
GO

-- ─── Unit Master: DefaultConversionFactor (fallback for product units) ───────
IF OBJECT_ID(N'dbo.Units', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Units', N'DefaultConversionFactor') IS NULL
    BEGIN
        IF COL_LENGTH(N'dbo.Units', N'ConversionFactor') IS NOT NULL
            EXEC sp_rename N'dbo.Units.ConversionFactor', N'DefaultConversionFactor', N'COLUMN';
        ELSE
            ALTER TABLE [dbo].[Units] ADD [DefaultConversionFactor] DECIMAL(18,4) NOT NULL
                CONSTRAINT [DF_Units_DefaultConversionFactor] DEFAULT 1;
    END

    UPDATE [dbo].[Units]
    SET [DefaultConversionFactor] = 1
    WHERE [DefaultConversionFactor] IS NULL OR [DefaultConversionFactor] <= 0;
END
GO

-- ─── Smart unit pricing ──────────────────────────────────────────────────────
IF COL_LENGTH(N'dbo.Products', N'UseAutoUnitPricing') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [UseAutoUnitPricing] BIT NOT NULL
        CONSTRAINT [DF_Products_UseAutoUnitPricing] DEFAULT 1;
END
GO

IF COL_LENGTH(N'dbo.ProductUnits', N'IsPriceOverridden') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductUnits] ADD [IsPriceOverridden] BIT NOT NULL
        CONSTRAINT [DF_ProductUnits_IsPriceOverridden] DEFAULT 0;
END
GO

-- Migrate legacy reciprocal factors (0.02 → 50, 0.05 → 20) to child-per-base format
UPDATE [dbo].[ProductUnits]
SET [ConversionFactor] = ROUND(1.0 / [ConversionFactor], 4)
WHERE [IsBaseUnit] = 0 AND [IsDeleted] = 0
  AND [ConversionFactor] > 0 AND [ConversionFactor] < 1;
GO

IF OBJECT_ID(N'dbo.Units', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Units]
    SET [DefaultConversionFactor] = ROUND(1.0 / [DefaultConversionFactor], 4)
    WHERE [IsDeleted] = 0
      AND [DefaultConversionFactor] > 0 AND [DefaultConversionFactor] < 1;
END
GO

PRINT 'Multi-unit inventory ALTER script completed.';
