using POSSystem.Domain;

namespace POSSystem.Application.Orders.DTOs;

public class CreateEmptyOrderDto
{
    public OrderType OrderType { get; set; }
    public int? TableId { get; set; }
    public int? CustomerId { get; set; }
    public int? WaiterId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int BranchId { get; set; }
}