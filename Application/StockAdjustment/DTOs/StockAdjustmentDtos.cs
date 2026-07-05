namespace POSSystem.Application.StockAdjustment.DTOs;

public class StockAdjustmentLineWriteDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int UnitId { get; set; }
    /// <summary>Signed quantity in selected unit (+ increase, − decrease).</summary>
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
}

public class CreateStockAdjustmentDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    public int WarehouseId { get; set; }
    public int AdjustmentTypeId { get; set; }
    public string? Remarks { get; set; }
    public int? CreatedBy { get; set; }
    public List<StockAdjustmentLineWriteDto> Lines { get; set; } = new();
}

public class UpdateStockAdjustmentDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    public int WarehouseId { get; set; }
    public int AdjustmentTypeId { get; set; }
    public string? Remarks { get; set; }
    public int? ModifiedBy { get; set; }
    public List<StockAdjustmentLineWriteDto> Lines { get; set; } = new();
}

public class StockAdjustmentFilterDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? WarehouseId { get; set; }
    public int? AdjustmentTypeId { get; set; }
    /// <summary>gain | loss | null for all</summary>
    public string? Direction { get; set; }
}

public class AdjustmentTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ExpenseAccountId { get; set; }
    public string ExpenseAccountName { get; set; } = string.Empty;
    public int IncomeAccountId { get; set; }
    public string IncomeAccountName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class StockAdjustmentLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string BaseUnitName { get; set; } = string.Empty;
    public decimal UnitQuantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal TotalCost { get; set; }
}

public class StockAdjustmentDto
{
    public int Id { get; set; }
    public string AdjustmentNo { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int AdjustmentTypeId { get; set; }
    public string AdjustmentTypeName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal GainAmount { get; set; }
    public decimal LossAmount { get; set; }
    public int LineCount { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
}

public class StockAdjustmentDetailDto : StockAdjustmentDto
{
    public List<StockAdjustmentLineDto> Lines { get; set; } = new();
}

public class StockAdjustmentReportRowDto
{
    public int Id { get; set; }
    public string AdjustmentNo { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string AdjustmentTypeName { get; set; } = string.Empty;
    public decimal GainAmount { get; set; }
    public decimal LossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public bool IsReversed { get; set; }
}
