using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        builder.ToTable("Sizes");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
        builder.Property(s => s.SortOrder).HasDefaultValue(0);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasIndex(s => new { s.BusinessId, s.BranchId, s.Name })
            .IsUnique()
            .HasDatabaseName("idx_size_business_branch_name");

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
