using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class StockTransferVoucherConfiguration : IEntityTypeConfiguration<StockTransferVoucher>
{
    public void Configure(EntityTypeBuilder<StockTransferVoucher> builder)
    {
        builder.ToTable("StockTransferVouchers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransferNo).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.TransferDate).HasColumnType("datetime2");
        builder.Property(x => x.IsReversed).HasDefaultValue(false);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.TransferNo })
            .IsUnique()
            .HasDatabaseName("idx_stock_transfer_no");

        builder.HasOne(x => x.FromWarehouse)
            .WithMany()
            .HasForeignKey(x => x.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToWarehouse)
            .WithMany()
            .HasForeignKey(x => x.ToWarehouseId)
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

public class StockTransferVoucherLineConfiguration : IEntityTypeConfiguration<StockTransferVoucherLine>
{
    public void Configure(EntityTypeBuilder<StockTransferVoucherLine> builder)
    {
        builder.ToTable("StockTransferVoucherLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ConversionFactor).HasPrecision(18, 4);

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
