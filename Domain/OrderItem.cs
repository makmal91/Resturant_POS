using System;

namespace POSSystem.Domain;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }
    public int? VariantId { get; set; }
    public string ModifiersJson { get; set; } = "[]";
    public string Notes { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual Order Order { get; set; } = null!;
    public virtual MenuItem MenuItem { get; set; } = null!;
}
