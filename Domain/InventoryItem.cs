using System.Collections.Generic;

namespace POSSystem.Domain;

public class InventoryItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal PurchasePrice { get; set; }
    public ProductType ProductType { get; set; } = ProductType.RawMaterial;
    public bool IsInventoryItem { get; set; } = true;
    public bool IsPurchasable { get; set; } = true;

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
