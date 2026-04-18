using System;

namespace POSSystem.Domain;

public class Recipe : BaseEntity
{
    public int MenuItemId { get; set; }
    public int IngredientId { get; set; }
    public decimal QuantityRequired { get; set; }
    public string Unit { get; set; } = string.Empty;

    public virtual Branch Branch { get; set; } = null!;
    public virtual MenuItem MenuItem { get; set; } = null!;
    public virtual InventoryItem Ingredient { get; set; } = null!;
}
