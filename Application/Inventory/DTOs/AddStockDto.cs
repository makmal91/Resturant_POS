using System.ComponentModel.DataAnnotations;

namespace POSSystem.Application.Inventory.DTOs;

public class AddStockDto
{
    [Required]
    public int ItemId { get; set; }
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }
    [Required]
    public int BranchId { get; set; }
}