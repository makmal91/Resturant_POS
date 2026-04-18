using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.QuantityRequired)
            .HasPrecision(10, 2);

        // Indexes
        builder.HasIndex(r => r.MenuItemId)
            .HasDatabaseName("idx_recipe_menuitemid");

        builder.HasIndex(r => r.IngredientId)
            .HasDatabaseName("idx_recipe_ingredientid");

        builder.HasIndex(r => r.BranchId)
            .HasDatabaseName("idx_recipe_branchid");

        builder.HasIndex(r => new { r.MenuItemId, r.IngredientId })
            .IsUnique()
            .HasDatabaseName("idx_recipe_menuitem_ingredient");

        // Relationships
        builder.HasOne(r => r.MenuItem)
            .WithMany(mi => mi.Recipes)
            .HasForeignKey(r => r.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Ingredient)
            .WithMany(ii => ii.Recipes)
            .HasForeignKey(r => r.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
