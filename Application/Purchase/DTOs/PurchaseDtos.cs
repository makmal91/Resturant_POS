using POSSystem.Domain;

namespace POSSystem.Application.Purchase.DTOs;

public class PurchaseDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime? VoidedAt    { get; set; }
    public string?  VoidedByName { get; set; }
}

public class PurchaseDetailDto : PurchaseDto
{
    public List<PurchaseItemDto> Items { get; set; } = new();
}

public class PurchaseItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal TotalCost { get; set; }
}

public class CreatePurchaseDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public List<CreatePurchaseItemDto> Items { get; set; } = new();
}

public class CreatePurchaseItemDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; } = 1;
    public decimal CostPrice { get; set; }
}

public class UpdatePurchaseDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public List<CreatePurchaseItemDto> Items { get; set; } = new();
}

public class PostPurchaseDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class VoidPurchaseDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public string? VoidedByName { get; set; }
    public string? Reason { get; set; }
}
