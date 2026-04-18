using System;
using System.Collections.Generic;

namespace POSSystem.Domain;

public class Order : BaseEntity
{
    public OrderType OrderType { get; set; }
    public int? TableId { get; set; }
    public int? CustomerId { get; set; }
    public int? WaiterId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string Notes { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual Table? Table { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual User? Waiter { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
