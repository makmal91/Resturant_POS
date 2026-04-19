using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class SubCategoryConfiguration : IEntityTypeConfiguration<SubCategory>
{
    public void Configure(EntityTypeBuilder<SubCategory> builder)
    {
        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sc => sc.Description)
            .HasMaxLength(500);

        builder.Property(sc => sc.Icon)
            .HasMaxLength(150);

        builder.Property(sc => sc.Status)
            .HasDefaultValue(true);

        builder.HasIndex(sc => sc.BranchId)
            .HasDatabaseName("idx_subcategory_branchid");

        builder.HasIndex(sc => new { sc.BranchId, sc.CategoryId })
            .HasDatabaseName("idx_subcategory_branch_category");

        builder.HasIndex(sc => new { sc.BranchId, sc.Name })
            .IsUnique()
            .HasDatabaseName("idx_subcategory_branch_name");

        builder.HasOne(sc => sc.Branch)
            .WithMany(b => b.SubCategories)
            .HasForeignKey(sc => sc.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sc => sc.Category)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(sc => sc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
