using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.HasKey(mc => mc.Id);

        builder.Property(mc => mc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(mc => mc.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(mc => mc.Description)
            .HasMaxLength(500);

        builder.Property(mc => mc.ImageUrl)
            .HasMaxLength(500);

        builder.Property(mc => mc.Icon)
            .HasMaxLength(150);

        builder.Property(mc => mc.Color)
            .HasMaxLength(50);

        builder.Property(mc => mc.Status)
            .HasDefaultValue(true);

        builder.Property(mc => mc.CategoryType)
            .HasConversion<int>()
            .HasDefaultValue(CategoryType.Sale);

        // Indexes
        builder.HasIndex(mc => mc.BranchId)
            .HasDatabaseName("idx_menucategory_branchid");

        builder.HasIndex(mc => new { mc.BranchId, mc.Name })
            .IsUnique()
            .HasDatabaseName("idx_menucategory_branch_name");

        builder.HasIndex(mc => new { mc.BranchId, mc.Code })
            .IsUnique()
            .HasDatabaseName("idx_menucategory_branch_code");

        builder.HasIndex(mc => new { mc.BranchId, mc.CategoryType })
            .HasDatabaseName("idx_menucategory_branch_type");

        // Relationships
        builder.HasMany(mc => mc.MenuItems)
            .WithOne(mi => mi.MenuCategory)
            .HasForeignKey(mi => mi.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(mc => mc.SubCategories)
            .WithOne(sc => sc.Category)
            .HasForeignKey(sc => sc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
