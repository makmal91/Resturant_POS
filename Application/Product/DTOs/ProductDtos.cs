using POSSystem.Domain;

namespace POSSystem.Application.Product.DTOs;

public class ProductSearchRequestDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? BrandId { get; set; }
    public bool? Status { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}

public class ProductListDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? SubCategoryId { get; set; }
    public string SubCategoryName { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public bool Status { get; set; }
    public bool HasImage { get; set; }
    public bool IsVariantEnabled { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool AllowNegativeStock { get; set; }
    public bool EnableLowStockAlert { get; set; }
    public decimal? LowStockAlertLevel { get; set; }
}

public class ProductDetailDto : ProductListDto
{
    public string Description { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public bool IsVariantEnabled { get; set; }
    public bool IsDiscountAllowed { get; set; }
    public ProductDiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public List<ProductUnitDto> Units { get; set; } = new();
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductBarcodeDto> Barcodes { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
    public decimal OpeningStock { get; set; }
    public bool HasOpeningStockApplied { get; set; }
    public bool OpeningStockVariantWise { get; set; }
    public List<ProductOpeningStockDto> OpeningStockByVariant { get; set; } = new();
}

public class ProductOpeningStockWriteDto
{
    public string VariantName { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public decimal Quantity { get; set; }
}

public class ProductOpeningStockDto : ProductOpeningStockWriteDto
{
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CreateProductDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? BrandId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public string ProductCode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public bool IsVariantEnabled { get; set; }
    public bool IsDiscountAllowed { get; set; }
    public ProductDiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public List<ProductUnitWriteDto> Units { get; set; } = new();
    public List<ProductVariantWriteDto> Variants { get; set; } = new();
    public List<ProductBarcodeWriteDto> Barcodes { get; set; } = new();
    public bool AllowNegativeStock { get; set; }
    public bool EnableLowStockAlert { get; set; }
    public decimal? LowStockAlertLevel { get; set; }
    public decimal OpeningStock { get; set; }
    public int? OpeningStockWarehouseId { get; set; }
    public bool OpeningStockVariantWise { get; set; }
    public List<ProductOpeningStockWriteDto> OpeningStockByVariant { get; set; } = new();
}

public class UpdateProductDto : CreateProductDto
{
}

public class ProductUnitWriteDto
{
    public int? Id { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1;
    public bool IsBaseUnit { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? WholesalePrice { get; set; }
}

public class ProductUnitDto : ProductUnitWriteDto
{
    public new int Id { get; set; }
}

public class ProductVariantWriteDto
{
    public int? Id { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
    public decimal? CostPriceOverride { get; set; }
    public decimal? SellingPriceOverride { get; set; }
    public bool Status { get; set; } = true;
}

public class ProductVariantDto : ProductVariantWriteDto
{
    public new int Id { get; set; }
}

public class ProductBarcodeWriteDto
{
    public int? Id { get; set; }
    public string BarcodeValue { get; set; } = string.Empty;
    public int? UnitId { get; set; }
    public int? VariantId { get; set; }
    public string? UnitName { get; set; }
    public string? VariantName { get; set; }
    public bool IsPrimary { get; set; }
}

public class ProductBarcodeDto : ProductBarcodeWriteDto
{
    public new int Id { get; set; }
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class ProductImageDataDto : ProductImageDto
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
}
