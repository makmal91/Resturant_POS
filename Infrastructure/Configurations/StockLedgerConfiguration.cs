using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class StockLedgerConfiguration : IEntityTypeConfiguration<StockLedger>
{
    public void Configure(EntityTypeBuilder<StockLedger> builder)
    {
        builder.ToTable("StockLedger");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.QuantityInBaseUnit).HasPrecision(18, 4);
        builder.Property(s => s.UnitQuantity).HasPrecision(18, 4);
        builder.Property(s => s.UnitPrice).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(s => s.Remarks).HasMaxLength(500);
        builder.Property(s => s.Date).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(s => new { s.VoucherId, s.Type })
            .HasDatabaseName("idx_ledger_voucher_type");

        builder.HasIndex(s => new { s.BusinessId, s.BranchId, s.ProductId, s.WarehouseId })
            .HasDatabaseName("idx_ledger_business_branch_product_warehouse");

        builder.HasIndex(s => new { s.BusinessId, s.BranchId, s.ProductId, s.VariantId, s.WarehouseId })
            .HasDatabaseName("idx_ledger_business_branch_product_variant_warehouse");

        builder.HasIndex(s => new { s.BusinessId, s.BranchId, s.Date })
            .HasDatabaseName("idx_ledger_business_branch_date");

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Variant)
            .WithMany()
            .HasForeignKey(s => s.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Unit)
            .WithMany()
            .HasForeignKey(s => s.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Warehouse)
            .WithMany(w => w.StockLedgerEntries)
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
