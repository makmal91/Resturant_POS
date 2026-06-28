using POSSystem.Application.Product.DTOs;

namespace POSSystem.Application.Barcode.DTOs;

public class BarcodePrintSearchRequestDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? BrandId { get; set; }
    public bool InStockOnly { get; set; }
}

public class BarcodePrintProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? PrimaryBarcode { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal StockQty { get; set; }
    public bool HasMultipleUnits { get; set; }
    public bool HasVariants { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? SubCategoryId { get; set; }
    public string SubCategoryName { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
}

public class ProductPrintDetailsDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public bool HasMultipleUnits { get; set; }
    public bool HasVariants { get; set; }
    public List<ProductUnitDto> Units { get; set; } = new();
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductBarcodeDto> Barcodes { get; set; } = new();
}
