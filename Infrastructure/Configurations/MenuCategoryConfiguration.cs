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

        builder.Property(mc => mc.Description)
            .HasMaxLength(500);

        builder.Property(mc => mc.CategoryType)
            .HasConversion<int>()
            .HasDefaultValue(CategoryType.Sale);

        // Indexes
        builder.HasIndex(mc => mc.BranchId)
            .HasDatabaseName("idx_menucategory_branchid");

        builder.HasIndex(mc => new { mc.BranchId, mc.Name })
            .IsUnique()
            .HasDatabaseName("idx_menucategory_branch_name");

        builder.HasIndex(mc => new { mc.BranchId, mc.CategoryType })
            .HasDatabaseName("idx_menucategory_branch_type");

        // Relationships
        builder.HasMany(mc => mc.MenuItems)
            .WithOne(mi => mi.MenuCategory)
            .HasForeignKey(mi => mi.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
