namespace POSSystem.Application.Orders.DTOs;

public class AddOrderItemDto
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; } = 0;
}