namespace POSSystem.Application.Orders.DTOs;

public class AddOrderItemDto
{
    public int MenuItemId { get; set; }
    public int? VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; } = 0;
    public List<int> AddOnIds { get; set; } = new List<int>();
    public List<int> ModifierIds { get; set; } = new List<int>();
    public string Notes { get; set; } = string.Empty;
}