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
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_product_business_branch_code' AND object_id = OBJECT_ID(N'[dbo].[Products]'))
                CREATE UNIQUE INDEX [idx_product_business_branch_code] ON [dbo].[Products]([BusinessId], [BranchId], [ProductCode]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_product_business_branch_sku' AND object_id = OBJECT_ID(N'[dbo].[Products]'))
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
}
