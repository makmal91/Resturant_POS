using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Description)
            .HasMaxLength(500);

        builder.Property(b => b.ImageData)
            .HasColumnType("varbinary(max)");

        builder.Property(b => b.ImageContentType)
            .HasMaxLength(100);

        builder.Property(b => b.ImageFileName)
            .HasMaxLength(255);

        builder.Property(b => b.Status)
            .HasDefaultValue(true);

        builder.HasIndex(b => b.BranchId)
            .HasDatabaseName("idx_brand_branchid");

        builder.HasIndex(b => new { b.BranchId, b.Name })
            .IsUnique()
            .HasDatabaseName("idx_brand_branch_name");

        builder.HasOne(b => b.Branch)
            .WithMany(br => br.Brands)
            .HasForeignKey(b => b.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
