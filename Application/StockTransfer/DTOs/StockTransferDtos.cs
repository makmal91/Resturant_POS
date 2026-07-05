namespace POSSystem.Application.StockTransfer.DTOs;

public class StockTransferLineWriteDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
}

public class CreateStockTransferVoucherDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public int? CreatedBy { get; set; }
    public List<StockTransferLineWriteDto> Lines { get; set; } = new();
}

public class UpdateStockTransferVoucherDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public int? ModifiedBy { get; set; }
    public List<StockTransferLineWriteDto> Lines { get; set; } = new();
}

public class ReverseStockTransferVoucherDto
{
    public int BusinessId { get; set; }
    public int BranchId { get; set; }
    public string? Reason { get; set; }
    public int? ReversedBy { get; set; }
}

public class StockTransferLineDto
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
    public string BaseUnitName { get; set; } = string.Empty;
}

public class StockTransferVoucherDto
{
    public int Id { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public string? Description { get; set; }
    public int FromWarehouseId { get; set; }
    public string FromWarehouseName { get; set; } = string.Empty;
    public int ToWarehouseId { get; set; }
    public string ToWarehouseName { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsReversed { get; set; }
    public DateTime? ReversedAt { get; set; }
}

public class StockTransferVoucherDetailDto : StockTransferVoucherDto
{
    public List<StockTransferLineDto> Lines { get; set; } = new();
}
