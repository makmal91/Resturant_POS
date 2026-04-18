using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.HasKey(mi => mi.Id);

        builder.Property(mi => mi.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(mi => mi.Description)
            .HasMaxLength(1000);

        builder.Property(mi => mi.Price)
            .HasPrecision(10, 2);

        builder.Property(mi => mi.CostPrice)
            .HasPrecision(10, 2);

        builder.Property(mi => mi.TaxPercentage)
            .HasPrecision(5, 2);

        builder.Property(mi => mi.ImageUrl)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(mi => mi.BranchId)
            .HasDatabaseName("idx_menuitem_branchid");

        builder.HasIndex(mi => mi.MenuCategoryId)
            .HasDatabaseName("idx_menuitem_categoryid");

        builder.HasIndex(mi => new { mi.BranchId, mi.IsAvailable })
            .HasDatabaseName("idx_menuitem_branch_available");

        // Relationships
        builder.HasOne(mi => mi.MenuCategory)
            .WithMany(mc => mc.MenuItems)
            .HasForeignKey(mi => mi.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(mi => mi.Variants)
            .WithOne(v => v.MenuItem)
            .HasForeignKey(v => v.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mi => mi.Recipes)
            .WithOne(r => r.MenuItem)
            .HasForeignKey(r => r.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
