using POSSystem.Domain;

namespace POSSystem.Application.Stock.DTOs;

public class StockBalanceDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    /// <summary>Stock quantity in base unit only.</summary>
    public decimal Quantity { get; set; }
    public string BaseUnitName { get; set; } = string.Empty;
    public int? BaseUnitId { get; set; }
    public List<StockUnitBreakdownDto> UnitBreakdown { get; set; } = new();
    public bool EnableLowStockAlert { get; set; }
    public decimal? LowStockAlertLevel { get; set; }
}

public class StockUnitBreakdownDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public bool IsBaseUnit { get; set; }
    public bool IsRemainder { get; set; }
}

public class StockLedgerDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public StockLedgerType Type { get; set; }
    public int? ReferenceId { get; set; }
    public decimal QuantityInBaseUnit { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal? UnitQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Date { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StockLedgerFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int? ProductId { get; set; }
    public int? VariantId { get; set; }
    public int? WarehouseId { get; set; }
    public StockLedgerType? Type { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class StockTransferDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
}

public class LowStockAlertDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal AlertLevel { get; set; }
    public DateTime LastTriggeredAt { get; set; }
}
