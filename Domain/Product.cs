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
    public bool IsVariantEnabled { get; set; }
    public bool IsDiscountAllowed { get; set; }
    public ProductDiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }

    public virtual MenuCategory Category { get; set; } = null!;
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
    public string UnitName { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1;
    public bool IsBaseUnit { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? WholesalePrice { get; set; }

    public virtual Product Product { get; set; } = null!;
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
