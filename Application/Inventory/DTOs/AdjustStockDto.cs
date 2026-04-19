using System.ComponentModel.DataAnnotations;

namespace POSSystem.Application.Inventory.DTOs;

public class AdjustStockDto
{
    [Required]
    public int ItemId { get; set; }

    // Positive increases stock, negative decreases stock.
    [Range(typeof(decimal), "-999999999", "999999999")]
    public decimal QuantityDelta { get; set; }

    [Required]
    public int BranchId { get; set; }

    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;
}
