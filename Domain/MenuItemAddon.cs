using System;

namespace POSSystem.Domain;

public class MenuItemAddon : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MenuItemId { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual MenuItem MenuItem { get; set; } = null!;
}