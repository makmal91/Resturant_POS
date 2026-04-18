using System.ComponentModel.DataAnnotations;

namespace POSSystem.Application.Inventory.DTOs;

public class TransferStockDto
{
    [Required]
    public int ItemId { get; set; }
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }
    [Required]
    public int FromBranchId { get; set; }
    [Required]
    public int ToBranchId { get; set; }
}