using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class ProductManagementDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Products] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProductName] NVARCHAR(200) NOT NULL,
                    [ProductCode] NVARCHAR(50) NOT NULL,
                    [SKU] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Products_SKU] DEFAULT N'',
                    [Description] NVARCHAR(1000) NOT NULL CONSTRAINT [DF_Products_Description] DEFAULT N'',
                    [Status] BIT NOT NULL CONSTRAINT [DF_Products_Status] DEFAULT 1,
                    [CategoryId] INT NOT NULL,
                    [SubCategoryId] INT NULL,
                    [BrandId] INT NULL,
                    [CostPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Products_CostPrice] DEFAULT 0,
                    [SellingPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Products_SellingPrice] DEFAULT 0,
                    [WholesalePrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Products_WholesalePrice] DEFAULT 0,
                    [IsVariantEnabled] BIT NOT NULL CONSTRAINT [DF_Products_IsVariantEnabled] DEFAULT 0,
                    [IsDiscountAllowed] BIT NOT NULL CONSTRAINT [DF_Products_IsDiscountAllowed] DEFAULT 0,
                    [DiscountType] INT NULL,
                    [DiscountValue] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Products_DiscountValue] DEFAULT 0,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_Products_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Products_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Products_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_Products_MenuCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[MenuCategories]([Id]),
                    CONSTRAINT [FK_Products_SubCategories_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [dbo].[SubCategories]([Id]),
                    CONSTRAINT [FK_Products_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [dbo].[Brands]([Id]),
                    CONSTRAINT [FK_Products_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[ProductUnits]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProductUnits] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProductId] INT NOT NULL,
                    [UnitName] NVARCHAR(100) NOT NULL,
                    [ConversionFactor] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_ProductUnits_ConversionFactor] DEFAULT 1,
                    [IsBaseUnit] BIT NOT NULL CONSTRAINT [DF_ProductUnits_IsBaseUnit] DEFAULT 0,
                    [CostPrice] DECIMAL(18,2) NULL,
                    [SellingPrice] DECIMAL(18,2) NULL,
                    [WholesalePrice] DECIMAL(18,2) NULL,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_ProductUnits_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_ProductUnits_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ProductUnits_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_ProductUnits_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
                );
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[ProductVariants]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProductVariants] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProductId] INT NOT NULL,
                    [VariantName] NVARCHAR(150) NOT NULL,
                    [Size] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ProductVariants_Size] DEFAULT N'',
                    [Color] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ProductVariants_Color] DEFAULT N'',
                    [SKU] NVARCHAR(100) NOT NULL CONSTRAINT [DF_ProductVariants_SKU] DEFAULT N'',
                    [AdditionalPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_ProductVariants_AdditionalPrice] DEFAULT 0,
                    [CostPriceOverride] DECIMAL(18,2) NULL,
                    [SellingPriceOverride] DECIMAL(18,2) NULL,
                    [Status] BIT NOT NULL CONSTRAINT [DF_ProductVariants_Status] DEFAULT 1,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_ProductVariants_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_ProductVariants_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ProductVariants_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_ProductVariants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
                );
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProductImages] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProductId] INT NOT NULL,
                    [FileName] NVARCHAR(255) NOT NULL CONSTRAINT [DF_ProductImages_FileName] DEFAULT N'',
                    [ContentType] NVARCHAR(100) NOT NULL CONSTRAINT [DF_ProductImages_ContentType] DEFAULT N'',
                    [ImageData] VARBINARY(MAX) NOT NULL,
                    [IsPrimary] BIT NOT NULL CONSTRAINT [DF_ProductImages_IsPrimary] DEFAULT 0,
                    [SortOrder] INT NOT NULL CONSTRAINT [DF_ProductImages_SortOrder] DEFAULT 0,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_ProductImages_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_ProductImages_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ProductImages_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
                );
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[ProductBarcodes]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProductBarcodes] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProductId] INT NOT NULL,
                    [ProductUnitId] INT NULL,
                    [ProductVariantId] INT NULL,
                    [BarcodeValue] NVARCHAR(100) NOT NULL,
                    [IsPrimary] BIT NOT NULL CONSTRAINT [DF_ProductBarcodes_IsPrimary] DEFAULT 0,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_ProductBarcodes_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_ProductBarcodes_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ProductBarcodes_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_ProductBarcodes_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_ProductBarcodes_ProductUnits_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [dbo].[ProductUnits]([Id]),
                    CONSTRAINT [FK_ProductBarcodes_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [dbo].[ProductVariants]([Id])
                );
            END
            """,
            SyncLegacyProductsSchemaSql(),
            SyncLegacyProductVariantsSchemaSql(),
            SyncLegacyProductUnitsSchemaSql(),
            SyncLegacyProductsBaseUnitSchemaSql(),
            SyncUnitPricingSchemaSql(),
            BackfillMultiUnitDataSql(),
            SyncDefaultSaleUnitSchemaSql(),
            SeedDefaultSaleUnitSql(),
            SyncLegacyProductImagesSchemaSql(),
            SyncLegacyProductBarcodesSchemaSql(),
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_product_business_branch_code' AND object_id = OBJECT_ID(N'[dbo].[Products]'))
                CREATE UNIQUE INDEX [idx_product_business_branch_code] ON [dbo].[Products]([BusinessId], [BranchId], [ProductCode]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_product_business_branch_sku' AND object_id = OBJECT_ID(N'[dbo].[Products]'))
               AND COL_LENGTH(N'dbo.Products', N'SKU') IS NOT NULL
                CREATE INDEX [idx_product_business_branch_sku] ON [dbo].[Products]([BusinessId], [BranchId], [SKU]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_product_business_branch_category' AND object_id = OBJECT_ID(N'[dbo].[Products]'))
                CREATE INDEX [idx_product_business_branch_category] ON [dbo].[Products]([BusinessId], [BranchId], [CategoryId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_productunit_product_name' AND object_id = OBJECT_ID(N'[dbo].[ProductUnits]'))
                CREATE INDEX [idx_productunit_product_name] ON [dbo].[ProductUnits]([ProductId], [UnitName]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_productvariant_product_sku' AND object_id = OBJECT_ID(N'[dbo].[ProductVariants]'))
                CREATE INDEX [idx_productvariant_product_sku] ON [dbo].[ProductVariants]([ProductId], [SKU]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_productimage_product_primary' AND object_id = OBJECT_ID(N'[dbo].[ProductImages]'))
                CREATE INDEX [idx_productimage_product_primary] ON [dbo].[ProductImages]([ProductId], [IsPrimary]);
            """,
            """
            -- Fix: recreate barcode unique index with soft-delete filter so updating a product
            -- (which soft-deletes old barcodes before inserting new ones) does not hit a duplicate key.
            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'idx_productbarcode_value'
                  AND object_id = OBJECT_ID(N'[dbo].[ProductBarcodes]')
                  AND has_filter = 0
            )
            BEGIN
                DROP INDEX [idx_productbarcode_value] ON [dbo].[ProductBarcodes];
            END
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'idx_productbarcode_value'
                  AND object_id = OBJECT_ID(N'[dbo].[ProductBarcodes]')
            )
            BEGIN
                CREATE UNIQUE INDEX [idx_productbarcode_value]
                    ON [dbo].[ProductBarcodes]([BarcodeValue])
                    WHERE [IsDeleted] = 0;
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
                logger.LogWarning(ex, "Product management schema batch skipped or partially applied.");
            }
        }
    }

    private static string SyncLegacyProductsSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Products', N'ProductName') IS NULL
               AND COL_LENGTH(N'dbo.Products', N'Name') IS NOT NULL
                EXEC sp_rename N'dbo.Products.Name', N'ProductName', N'COLUMN';

            IF COL_LENGTH(N'dbo.Products', N'CostPrice') IS NULL
               AND COL_LENGTH(N'dbo.Products', N'PurchasePrice') IS NOT NULL
                EXEC sp_rename N'dbo.Products.PurchasePrice', N'CostPrice', N'COLUMN';

            IF COL_LENGTH(N'dbo.Products', N'SellingPrice') IS NULL
               AND COL_LENGTH(N'dbo.Products', N'SalePrice') IS NOT NULL
                EXEC sp_rename N'dbo.Products.SalePrice', N'SellingPrice', N'COLUMN';

            IF COL_LENGTH(N'dbo.Products', N'Status') IS NULL
               AND COL_LENGTH(N'dbo.Products', N'IsActive') IS NOT NULL
                EXEC sp_rename N'dbo.Products.IsActive', N'Status', N'COLUMN';

            IF COL_LENGTH(N'dbo.Products', N'IsVariantEnabled') IS NULL
               AND COL_LENGTH(N'dbo.Products', N'HasVariants') IS NOT NULL
                EXEC sp_rename N'dbo.Products.HasVariants', N'IsVariantEnabled', N'COLUMN';

            IF COL_LENGTH(N'dbo.Products', N'ProductName') IS NULL
                ALTER TABLE [dbo].[Products] ADD [ProductName] NVARCHAR(200) NOT NULL
                    CONSTRAINT [DF_Products_ProductName_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Products', N'CostPrice') IS NULL
                ALTER TABLE [dbo].[Products] ADD [CostPrice] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_Products_CostPrice_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.Products', N'SellingPrice') IS NULL
                ALTER TABLE [dbo].[Products] ADD [SellingPrice] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_Products_SellingPrice_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.Products', N'WholesalePrice') IS NULL
                ALTER TABLE [dbo].[Products] ADD [WholesalePrice] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_Products_WholesalePrice_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.Products', N'SKU') IS NULL
                ALTER TABLE [dbo].[Products] ADD [SKU] NVARCHAR(100) NOT NULL
                    CONSTRAINT [DF_Products_SKU_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.Products', N'Status') IS NULL
                ALTER TABLE [dbo].[Products] ADD [Status] BIT NOT NULL
                    CONSTRAINT [DF_Products_Status_Legacy] DEFAULT 1;

            IF COL_LENGTH(N'dbo.Products', N'IsVariantEnabled') IS NULL
                ALTER TABLE [dbo].[Products] ADD [IsVariantEnabled] BIT NOT NULL
                    CONSTRAINT [DF_Products_IsVariantEnabled_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.Products', N'IsDiscountAllowed') IS NULL
                ALTER TABLE [dbo].[Products] ADD [IsDiscountAllowed] BIT NOT NULL
                    CONSTRAINT [DF_Products_IsDiscountAllowed_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.Products', N'DiscountType') IS NULL
                ALTER TABLE [dbo].[Products] ADD [DiscountType] INT NULL;

            IF COL_LENGTH(N'dbo.Products', N'DiscountValue') IS NULL
                ALTER TABLE [dbo].[Products] ADD [DiscountValue] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_Products_DiscountValue_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.Products', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[Products] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.Products', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[Products] ADD [ModifiedByName] NVARCHAR(MAX) NULL;
        END
        """;

    private static string SyncLegacyProductVariantsSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[ProductVariants]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.ProductVariants', N'CostPriceOverride') IS NULL
               AND COL_LENGTH(N'dbo.ProductVariants', N'PurchasePrice') IS NOT NULL
                EXEC sp_rename N'dbo.ProductVariants.PurchasePrice', N'CostPriceOverride', N'COLUMN';

            IF COL_LENGTH(N'dbo.ProductVariants', N'SellingPriceOverride') IS NULL
               AND COL_LENGTH(N'dbo.ProductVariants', N'SalePrice') IS NOT NULL
                EXEC sp_rename N'dbo.ProductVariants.SalePrice', N'SellingPriceOverride', N'COLUMN';

            IF COL_LENGTH(N'dbo.ProductVariants', N'Status') IS NULL
               AND COL_LENGTH(N'dbo.ProductVariants', N'IsActive') IS NOT NULL
                EXEC sp_rename N'dbo.ProductVariants.IsActive', N'Status', N'COLUMN';

            IF COL_LENGTH(N'dbo.ProductVariants', N'CostPriceOverride') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [CostPriceOverride] DECIMAL(18,2) NULL;

            IF COL_LENGTH(N'dbo.ProductVariants', N'SellingPriceOverride') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [SellingPriceOverride] DECIMAL(18,2) NULL;

            IF COL_LENGTH(N'dbo.ProductVariants', N'Size') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [Size] NVARCHAR(50) NOT NULL
                    CONSTRAINT [DF_ProductVariants_Size_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.ProductVariants', N'Color') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [Color] NVARCHAR(50) NOT NULL
                    CONSTRAINT [DF_ProductVariants_Color_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.ProductVariants', N'AdditionalPrice') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [AdditionalPrice] DECIMAL(18,2) NOT NULL
                    CONSTRAINT [DF_ProductVariants_AdditionalPrice_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.ProductVariants', N'Status') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [Status] BIT NOT NULL
                    CONSTRAINT [DF_ProductVariants_Status_Legacy] DEFAULT 1;

            IF COL_LENGTH(N'dbo.ProductVariants', N'SKU') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [SKU] NVARCHAR(100) NOT NULL
                    CONSTRAINT [DF_ProductVariants_SKU_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.ProductVariants', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.ProductVariants', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[ProductVariants] ADD [ModifiedByName] NVARCHAR(MAX) NULL;
        END
        """;

    private static string SyncLegacyProductUnitsSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[ProductUnits]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.ProductUnits', N'UnitName') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [UnitName] NVARCHAR(100) NOT NULL
                    CONSTRAINT [DF_ProductUnits_UnitName_Legacy] DEFAULT N'Unit';

            IF COL_LENGTH(N'dbo.ProductUnits', N'CostPrice') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [CostPrice] DECIMAL(18,2) NULL;

            IF COL_LENGTH(N'dbo.ProductUnits', N'SellingPrice') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [SellingPrice] DECIMAL(18,2) NULL;

            IF COL_LENGTH(N'dbo.ProductUnits', N'WholesalePrice') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [WholesalePrice] DECIMAL(18,2) NULL;

            IF COL_LENGTH(N'dbo.ProductUnits', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.ProductUnits', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [ModifiedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.ProductUnits', N'UnitId') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [UnitId] INT NULL;

            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = N'FK_ProductUnits_Units_UnitId'
                  AND parent_object_id = OBJECT_ID(N'dbo.ProductUnits')
            )
                ALTER TABLE [dbo].[ProductUnits]
                    ADD CONSTRAINT [FK_ProductUnits_Units_UnitId]
                    FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]);
        END
        """;

    private static string SyncLegacyProductsBaseUnitSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Products', N'BaseUnitId') IS NULL
                ALTER TABLE [dbo].[Products] ADD [BaseUnitId] INT NULL;

            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = N'FK_Products_ProductUnits_BaseUnitId'
                  AND parent_object_id = OBJECT_ID(N'dbo.Products')
            )
                ALTER TABLE [dbo].[Products]
                    ADD CONSTRAINT [FK_Products_ProductUnits_BaseUnitId]
                    FOREIGN KEY ([BaseUnitId]) REFERENCES [dbo].[ProductUnits]([Id]);

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'idx_product_base_unit_id'
                  AND object_id = OBJECT_ID(N'dbo.Products')
            )
                CREATE INDEX [idx_product_base_unit_id] ON [dbo].[Products]([BaseUnitId]);
        END
        """;

    private static string SyncUnitPricingSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Products', N'UseAutoUnitPricing') IS NULL
                ALTER TABLE [dbo].[Products] ADD [UseAutoUnitPricing] BIT NOT NULL
                    CONSTRAINT [DF_Products_UseAutoUnitPricing] DEFAULT 1;
        END

        IF OBJECT_ID(N'[dbo].[ProductUnits]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.ProductUnits', N'IsPriceOverridden') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [IsPriceOverridden] BIT NOT NULL
                    CONSTRAINT [DF_ProductUnits_IsPriceOverridden] DEFAULT 0;

            -- Legacy factors stored as reciprocals (e.g. 0.02 instead of 50) → child-per-base format
            UPDATE [dbo].[ProductUnits]
            SET [ConversionFactor] = ROUND(1.0 / [ConversionFactor], 4)
            WHERE [IsBaseUnit] = 0 AND [IsDeleted] = 0
              AND [ConversionFactor] > 0 AND [ConversionFactor] < 1;
        END

        IF OBJECT_ID(N'[dbo].[Units]', N'U') IS NOT NULL
        BEGIN
            UPDATE [dbo].[Units]
            SET [DefaultConversionFactor] = ROUND(1.0 / [DefaultConversionFactor], 4)
            WHERE [IsDeleted] = 0
              AND [DefaultConversionFactor] > 0 AND [DefaultConversionFactor] < 1;
        END
        """;

    // NOTE: Adding the IsDefaultSaleUnit column and referencing it must happen in SEPARATE
    // batches. SQL Server compiles a whole batch before executing it, so an ALTER TABLE ADD
    // followed by a statement that reads the new column in the same batch throws a compile-time
    // "Invalid column name" and aborts the entire batch (column never gets added). The default
    // seeding lives in SeedDefaultSaleUnitSql(), executed as a later batch.
    private static string SyncDefaultSaleUnitSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[ProductUnits]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.ProductUnits', N'IsDefaultSaleUnit') IS NULL
                ALTER TABLE [dbo].[ProductUnits] ADD [IsDefaultSaleUnit] BIT NOT NULL
                    CONSTRAINT [DF_ProductUnits_IsDefaultSaleUnit] DEFAULT 0;

            -- Prices are now fully manual per unit. Backfill any NULL prices from the
            -- previous auto-pricing formula (base price ÷ conversion factor) so existing
            -- products keep their effective prices when auto-calculation is removed.
            UPDATE pu
            SET pu.[CostPrice] = CASE WHEN pu.[IsBaseUnit] = 1 THEN p.[CostPrice]
                                      ELSE ROUND(p.[CostPrice] / NULLIF(pu.[ConversionFactor], 0), 2) END
            FROM [dbo].[ProductUnits] pu
            INNER JOIN [dbo].[Products] p ON p.[Id] = pu.[ProductId]
            WHERE pu.[IsDeleted] = 0 AND pu.[CostPrice] IS NULL;

            UPDATE pu
            SET pu.[SellingPrice] = CASE WHEN pu.[IsBaseUnit] = 1 THEN p.[SellingPrice]
                                         ELSE ROUND(p.[SellingPrice] / NULLIF(pu.[ConversionFactor], 0), 2) END
            FROM [dbo].[ProductUnits] pu
            INNER JOIN [dbo].[Products] p ON p.[Id] = pu.[ProductId]
            WHERE pu.[IsDeleted] = 0 AND pu.[SellingPrice] IS NULL;

            UPDATE pu
            SET pu.[WholesalePrice] = CASE WHEN pu.[IsBaseUnit] = 1 THEN p.[WholesalePrice]
                                           ELSE ROUND(p.[WholesalePrice] / NULLIF(pu.[ConversionFactor], 0), 2) END
            FROM [dbo].[ProductUnits] pu
            INNER JOIN [dbo].[Products] p ON p.[Id] = pu.[ProductId]
            WHERE pu.[IsDeleted] = 0 AND pu.[WholesalePrice] IS NULL;
        END
        """;

    // Seeds exactly one default sale unit per product. Runs as its own batch AFTER the column
    // exists (see note on SyncDefaultSaleUnitSchemaSql). Guarded so it no-ops if the column is
    // somehow still missing.
    private static string SeedDefaultSaleUnitSql() => """
        IF OBJECT_ID(N'[dbo].[ProductUnits]', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ProductUnits', N'IsDefaultSaleUnit') IS NOT NULL
        BEGIN
            -- Prefer the existing base unit as the default sale unit.
            UPDATE pu
            SET pu.[IsDefaultSaleUnit] = 1
            FROM [dbo].[ProductUnits] pu
            WHERE pu.[IsBaseUnit] = 1 AND pu.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[ProductUnits] d
                  WHERE d.[ProductId] = pu.[ProductId] AND d.[IsDeleted] = 0 AND d.[IsDefaultSaleUnit] = 1);

            -- Products with no base unit flagged: fall back to the lowest ProductUnit id.
            UPDATE pu
            SET pu.[IsDefaultSaleUnit] = 1
            FROM [dbo].[ProductUnits] pu
            WHERE pu.[IsDeleted] = 0
              AND pu.[Id] = (
                  SELECT MIN(d.[Id]) FROM [dbo].[ProductUnits] d
                  WHERE d.[ProductId] = pu.[ProductId] AND d.[IsDeleted] = 0)
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[ProductUnits] d2
                  WHERE d2.[ProductId] = pu.[ProductId] AND d2.[IsDeleted] = 0 AND d2.[IsDefaultSaleUnit] = 1);
        END
        """;

    private static string BackfillMultiUnitDataSql() => """
        IF OBJECT_ID(N'[dbo].[ProductUnits]', N'U') IS NOT NULL
        BEGIN
            UPDATE pu
            SET pu.[UnitId] = u.[Id]
            FROM [dbo].[ProductUnits] pu
            INNER JOIN [dbo].[Units] u
                ON u.[BusinessId] = pu.[BusinessId]
               AND u.[BranchId] = pu.[BranchId]
               AND LTRIM(RTRIM(u.[Name])) = LTRIM(RTRIM(pu.[UnitName]))
               AND u.[IsDeleted] = 0
            WHERE pu.[UnitId] IS NULL AND pu.[IsDeleted] = 0;

            UPDATE [dbo].[ProductUnits]
            SET [ConversionFactor] = 1
            WHERE [IsBaseUnit] = 1 AND [IsDeleted] = 0 AND [ConversionFactor] <> 1;
        END

        IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
        BEGIN
            UPDATE p
            SET p.[BaseUnitId] = bu.[Id]
            FROM [dbo].[Products] p
            INNER JOIN [dbo].[ProductUnits] bu
                ON bu.[ProductId] = p.[Id]
               AND bu.[IsBaseUnit] = 1
               AND bu.[IsDeleted] = 0
            WHERE p.[BaseUnitId] IS NULL AND p.[IsDeleted] = 0;
        END
        """;

    private static string SyncLegacyProductImagesSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[ProductImages]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.ProductImages', N'FileName') IS NULL
               AND COL_LENGTH(N'dbo.ProductImages', N'ImageFileName') IS NOT NULL
                EXEC sp_rename N'dbo.ProductImages.ImageFileName', N'FileName', N'COLUMN';

            IF COL_LENGTH(N'dbo.ProductImages', N'ContentType') IS NULL
               AND COL_LENGTH(N'dbo.ProductImages', N'ImageContentType') IS NOT NULL
                EXEC sp_rename N'dbo.ProductImages.ImageContentType', N'ContentType', N'COLUMN';

            IF COL_LENGTH(N'dbo.ProductImages', N'FileName') IS NULL
                ALTER TABLE [dbo].[ProductImages] ADD [FileName] NVARCHAR(255) NOT NULL
                    CONSTRAINT [DF_ProductImages_FileName_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.ProductImages', N'ContentType') IS NULL
                ALTER TABLE [dbo].[ProductImages] ADD [ContentType] NVARCHAR(100) NOT NULL
                    CONSTRAINT [DF_ProductImages_ContentType_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.ProductImages', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[ProductImages] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.ProductImages', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[ProductImages] ADD [ModifiedByName] NVARCHAR(MAX) NULL;
        END
        """;

    private static string SyncLegacyProductBarcodesSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[ProductBarcodes]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.ProductBarcodes', N'BarcodeValue') IS NULL
               AND COL_LENGTH(N'dbo.ProductBarcodes', N'Barcode') IS NOT NULL
                EXEC sp_rename N'dbo.ProductBarcodes.Barcode', N'BarcodeValue', N'COLUMN';

            IF COL_LENGTH(N'dbo.ProductBarcodes', N'BarcodeValue') IS NULL
                ALTER TABLE [dbo].[ProductBarcodes] ADD [BarcodeValue] NVARCHAR(100) NOT NULL
                    CONSTRAINT [DF_ProductBarcodes_BarcodeValue_Legacy] DEFAULT N'';

            IF COL_LENGTH(N'dbo.ProductBarcodes', N'ProductUnitId') IS NULL
                ALTER TABLE [dbo].[ProductBarcodes] ADD [ProductUnitId] INT NULL;

            IF COL_LENGTH(N'dbo.ProductBarcodes', N'ProductVariantId') IS NULL
                ALTER TABLE [dbo].[ProductBarcodes] ADD [ProductVariantId] INT NULL;

            IF COL_LENGTH(N'dbo.ProductBarcodes', N'IsPrimary') IS NULL
                ALTER TABLE [dbo].[ProductBarcodes] ADD [IsPrimary] BIT NOT NULL
                    CONSTRAINT [DF_ProductBarcodes_IsPrimary_Legacy] DEFAULT 0;

            IF COL_LENGTH(N'dbo.ProductBarcodes', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[ProductBarcodes] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.ProductBarcodes', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[ProductBarcodes] ADD [ModifiedByName] NVARCHAR(MAX) NULL;
        END
        """;
}
