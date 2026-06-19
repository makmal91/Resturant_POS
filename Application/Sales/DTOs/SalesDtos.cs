using POSSystem.Application.Common.DTOs;
using POSSystem.Domain;

namespace POSSystem.Application.Sales.DTOs;

// ─── Product Lookup ───────────────────────────────────────────────────────────

public class PosProductLookupDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public bool IsVariantEnabled { get; set; }
    public bool IsDiscountAllowed { get; set; }
    public ProductDiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public int? MatchedUnitId { get; set; }
    public string MatchedUnitName { get; set; } = string.Empty;
    public decimal MatchedUnitConversionFactor { get; set; } = 1;
    public int? MatchedVariantId { get; set; }
    public string? MatchedVariantName { get; set; }
    public string? MatchedVariantSize { get; set; }
    public string? MatchedVariantColor { get; set; }
    public decimal? MatchedVariantSellingPrice { get; set; }
    public decimal Stock { get; set; }
    public bool AllowNegativeStock { get; set; }
    public string BaseUnitName { get; set; } = string.Empty;
    public List<PosProductUnitDto> AvailableUnits { get; set; } = new();
    public List<PosProductVariantDto> AvailableVariants { get; set; } = new();
}

public class PosProductUnitDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal ConversionFactor { get; set; }
    public bool IsBaseUnit { get; set; }
}

public class PosProductVariantDto
{
    public int VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal SellingPriceOverride { get; set; }
    public decimal AdditionalPrice { get; set; }
}

// ─── Customer Search ──────────────────────────────────────────────────────────

public class PosCustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// ─── Grouped Product Search (POS) ─────────────────────────────────────────────

public class PosSearchGroupDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public bool IsVariantEnabled { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal Stock { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool IsDiscountAllowed { get; set; }
    public ProductDiscountType? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public List<PosProductUnitDto> Units { get; set; } = new();
    public List<PosSearchVariantRowDto> Variants { get; set; } = new();
}

public class PosSearchVariantRowDto
{
    public int VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal Stock { get; set; }
}

// ─── Sale Invoice ─────────────────────────────────────────────────────────────

public class CreateSaleInvoiceDto
{
    public int? CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public PricingType PricingType { get; set; } = PricingType.Retail;
    public SalePaymentMethod PaymentMethod { get; set; } = SalePaymentMethod.Cash;
    public decimal PaidAmount { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public string? CashierName { get; set; }
    public bool IsCreditSale { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public List<CreateSaleInvoiceItemDto> Items { get; set; } = new();
}

public class CreateSaleInvoiceItemDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public string? ItemNote { get; set; }
}

// ─── Hold Bill ────────────────────────────────────────────────────────────────

public class HoldBillDto
{
    public string? HeldNote { get; set; }
    public int? CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public PricingType PricingType { get; set; } = PricingType.Retail;
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public List<CreateSaleInvoiceItemDto> Items { get; set; } = new();
}

// ─── Sale Invoice Result DTOs ─────────────────────────────────────────────────

public class SaleInvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public decimal ReturnAmount { get; set; }
    public SalePaymentMethod PaymentMethod { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public SaleInvoiceStatus Status { get; set; }
    public PricingType PricingType { get; set; }
    public string? Notes { get; set; }
    public string? HeldNote { get; set; }
    public string? CashierName { get; set; }
    public DateTime? VoidedAt    { get; set; }
    public string?  VoidedByName { get; set; }
    public bool IsCreditSale { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public string BranchPhone { get; set; } = string.Empty;
    public string BranchEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SaleInvoiceItemDto> Items { get; set; } = new();
}

public class SaleInvoiceItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public string? VariantSize { get; set; }
    public string? VariantColor { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? ItemNote { get; set; }
}

// ─── Invoice History / Paged Query ───────────────────────────────────────────

public class SaleInvoiceFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public SaleInvoiceStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class SaleInvoiceListDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public bool IsCreditSale { get; set; }
    public SaleInvoiceStatus Status { get; set; }
    public string? CashierName { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int BranchId { get; set; }
    public int WarehouseId { get; set; }
    public int? CustomerId { get; set; }
}

// ─── Transaction Correction DTOs ─────────────────────────────────────────────

public class UpdateSaleInvoiceDto
{
    public int? CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public PricingType PricingType { get; set; } = PricingType.Retail;
    public SalePaymentMethod PaymentMethod { get; set; } = SalePaymentMethod.Cash;
    public decimal PaidAmount { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public string? CashierName { get; set; }
    public bool IsCreditSale { get; set; }
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public List<CreateSaleInvoiceItemDto> Items { get; set; } = new();
}

public class VoidSaleInvoiceDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public string? VoidedByName { get; set; }
    public string? Reason { get; set; }
}

public class SaleLedgerEntryDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityInBaseUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
