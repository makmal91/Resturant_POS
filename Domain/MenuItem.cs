using System.Collections.Generic;

namespace POSSystem.Domain;

public class MenuItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public decimal TaxPercentage { get; set; }
    public int PreparationTime { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public bool IsVeg { get; set; }
    public int MenuCategoryId { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual MenuCategory MenuCategory { get; set; } = null!;
    public virtual ICollection<MenuItemVariant> Variants { get; set; } = new List<MenuItemVariant>();
    public virtual ICollection<MenuItemAddon> Addons { get; set; } = new List<MenuItemAddon>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
