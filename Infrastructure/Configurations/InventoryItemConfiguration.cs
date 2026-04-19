using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.CurrentStock)
            .HasPrecision(12, 2);

        builder.Property(i => i.MinStockLevel)
            .HasPrecision(12, 2);

        builder.Property(i => i.PurchasePrice)
            .HasPrecision(10, 2);

        builder.Property(i => i.ProductType)
            .HasConversion<int>()
            .HasDefaultValue(ProductType.RawMaterial);

        builder.Property(i => i.IsInventoryItem)
            .HasDefaultValue(true);

        builder.Property(i => i.IsPurchasable)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(i => i.BranchId)
            .HasDatabaseName("idx_inventoryitem_branchid");

        builder.HasIndex(i => new { i.BranchId, i.Name })
            .IsUnique()
            .HasDatabaseName("idx_inventoryitem_branch_name");

        builder.HasIndex(i => i.Category)
            .HasDatabaseName("idx_inventoryitem_category");

        builder.HasIndex(i => new { i.BranchId, i.IsInventoryItem, i.ProductType })
            .HasDatabaseName("idx_inventoryitem_branch_inventory_type");

        // Relationships
        builder.HasMany(i => i.StockMovements)
            .WithOne(sm => sm.Item)
            .HasForeignKey(sm => sm.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Recipes)
            .WithOne(r => r.Ingredient)
            .HasForeignKey(r => r.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
