using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class LowStockAlertConfiguration : IEntityTypeConfiguration<LowStockAlert>
{
    public void Configure(EntityTypeBuilder<LowStockAlert> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CurrentStock).HasPrecision(18, 4);
        builder.Property(a => a.AlertLevel).HasPrecision(18, 4);
        builder.Property(a => a.IsActive).HasDefaultValue(true);

        builder.HasIndex(a => new { a.BusinessId, a.BranchId, a.ProductId, a.VariantId, a.WarehouseId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("idx_lowstockalert_product_variant_warehouse");

        builder.HasOne(a => a.Product)
            .WithMany()
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Variant)
            .WithMany()
            .HasForeignKey(a => a.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Warehouse)
            .WithMany()
            .HasForeignKey(a => a.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
