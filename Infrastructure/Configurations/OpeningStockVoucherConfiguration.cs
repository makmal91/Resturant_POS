using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class OpeningStockVoucherConfiguration : IEntityTypeConfiguration<OpeningStockVoucher>
{
    public void Configure(EntityTypeBuilder<OpeningStockVoucher> builder)
    {
        builder.ToTable("OpeningStockVouchers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VoucherNo).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.VoucherDate).HasColumnType("datetime2");
        builder.Property(x => x.IsReversed).HasDefaultValue(false);

        builder.HasOne(x => x.ReferenceVoucher)
            .WithMany()
            .HasForeignKey(x => x.ReferenceVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReversalVoucher)
            .WithMany()
            .HasForeignKey(x => x.ReversalVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.VoucherNo })
            .IsUnique()
            .HasDatabaseName("idx_opening_stock_voucher_no");

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Voucher)
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OpeningStockVoucherLineConfiguration : IEntityTypeConfiguration<OpeningStockVoucherLine>
{
    public void Configure(EntityTypeBuilder<OpeningStockVoucherLine> builder)
    {
        builder.ToTable("OpeningStockVoucherLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ConversionFactor).HasPrecision(18, 4);
        builder.Property(x => x.CostPrice).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Variant)
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
