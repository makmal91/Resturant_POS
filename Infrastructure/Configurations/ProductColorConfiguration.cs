using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class ProductColorConfiguration : IEntityTypeConfiguration<ProductColor>
{
    public void Configure(EntityTypeBuilder<ProductColor> builder)
    {
        builder.ToTable("Colors");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.Property(c => c.HexCode).HasMaxLength(7);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasIndex(c => new { c.BusinessId, c.BranchId, c.Name })
            .IsUnique()
            .HasDatabaseName("idx_color_business_branch_name");

        builder.HasOne(c => c.Branch)
            .WithMany()
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
