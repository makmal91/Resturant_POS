using System.Collections.Generic;

namespace POSSystem.Domain;

public enum ProductDiscountType
{
    Percentage = 1,
    Fixed = 2
}

public class Product : BaseEntity
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public int CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? BrandId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    /// <summary>When true, alternate unit prices are auto-calculated from base price × conversion factor.</summary>
    public bool UseAutoUnitPricing { get; set; } = true;
    public bool IsVariantEnabled { get; set; }
    public bool IsDiscountAllowed { get; set; }
    public ProductDiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool EnableLowStockAlert { get; set; }
    public decimal? LowStockAlertLevel { get; set; }
    public decimal OpeningStock { get; set; }
    public bool OpeningStockVariantWise { get; set; }
    /// <summary>FK to the product's base <see cref="ProductUnit"/> row. Stock is always stored in this unit.</summary>
    public int? BaseUnitId { get; set; }

    public virtual MenuCategory Category { get; set; } = null!;
    public virtual ProductUnit? BaseUnit { get; set; }
    public virtual SubCategory? SubCategory { get; set; }
    public virtual Brand? Brand { get; set; }
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<ProductUnit> Units { get; set; } = new List<ProductUnit>();
    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public virtual ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}

public class ProductUnit : BaseEntity
{
    public int ProductId { get; set; }
    /// <summary>Optional FK to branch Unit Master (<see cref="MeasurementUnit"/>).</summary>
    public int? UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    /// <summary>Number of base units contained in 1 of this unit. Base unit must be 1.</summary>
    public decimal ConversionFactor { get; set; } = 1;
    public bool IsBaseUnit { get; set; }
    /// <summary>When true, this unit is pre-selected on the POS when the product is picked manually.
    /// Only one unit per product may be the default sale unit.</summary>
    public bool IsDefaultSaleUnit { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? WholesalePrice { get; set; }
    /// <summary>When true, stored prices are manual overrides; auto calculation is skipped.</summary>
    public bool IsPriceOverridden { get; set; }

    public virtual Product Product { get; set; } = null!;
    public virtual MeasurementUnit? Unit { get; set; }
}

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
    public decimal? CostPriceOverride { get; set; }
    public decimal? SellingPriceOverride { get; set; }
    public bool Status { get; set; } = true;

    public virtual Product Product { get; set; } = null!;
}

public class ProductBarcode : BaseEntity
{
    public int ProductId { get; set; }
    public int? ProductUnitId { get; set; }
    public int? ProductVariantId { get; set; }
    public string BarcodeValue { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }

    public virtual Product Product { get; set; } = null!;
    public virtual ProductUnit? ProductUnit { get; set; }
    public virtual ProductVariant? ProductVariant { get; set; }
}

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    public virtual Product Product { get; set; } = null!;
}
