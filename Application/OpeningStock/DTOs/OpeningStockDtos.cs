namespace POSSystem.Application.OpeningStock.DTOs;

public class OpeningStockLineWriteDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int UnitId { get; set; }
    /// <summary>Quantity in the selected unit (not base unit).</summary>
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
}

public class CreateOpeningStockVoucherDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public string? VoucherNo { get; set; }
    public DateTime VoucherDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public int WarehouseId { get; set; }
    public int? CreatedBy { get; set; }
    public List<OpeningStockLineWriteDto> Lines { get; set; } = new();
}

public class UpdateOpeningStockVoucherDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public DateTime VoucherDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public int WarehouseId { get; set; }
    public int? ModifiedBy { get; set; }
    public List<OpeningStockLineWriteDto> Lines { get; set; } = new();
}

public class ReverseOpeningStockVoucherDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public string? Reason { get; set; }
    public int? ReversedBy { get; set; }
}

public class OpeningStockLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public int? UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal UnitQuantity { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string BaseUnitName { get; set; } = string.Empty;
}

public class OpeningStockVoucherDto
{
    public int Id { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public string? Description { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
    public int? ReferenceVoucherId { get; set; }
    public int? ReversalVoucherId { get; set; }
}

public class OpeningStockVoucherDetailDto : OpeningStockVoucherDto
{
    public List<OpeningStockLineDto> Lines { get; set; } = new();
}
