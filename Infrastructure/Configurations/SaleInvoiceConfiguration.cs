using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class SaleInvoiceConfiguration : IEntityTypeConfiguration<SaleInvoice>
{
    public void Configure(EntityTypeBuilder<SaleInvoice> builder)
    {
        builder.ToTable("SaleInvoices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNo).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SettlementStatus).HasConversion<int>().HasDefaultValue(InvoiceSettlementStatus.Pending);
        builder.Property(x => x.ReturnAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CashAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CardAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.HeldNote).HasMaxLength(500);
        builder.Property(x => x.CashierName).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.PaymentMethod).HasConversion<int>();
        builder.Property(x => x.PricingType).HasConversion<int>();
        builder.Property(x => x.IsCreditSale).HasDefaultValue(false);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.SaleInvoice)
            .HasForeignKey(i => i.SaleInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.InvoiceNo })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.Status });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.SaleDate });
    }
}

public class SaleInvoiceItemConfiguration : IEntityTypeConfiguration<SaleInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SaleInvoiceItem> builder)
    {
        builder.ToTable("SaleInvoiceItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ConversionFactor).HasColumnType("decimal(18,6)");
        builder.Property(x => x.BaseQuantity).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountPercent).HasColumnType("decimal(8,4)");
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxPercent).HasColumnType("decimal(8,4)");
        builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ItemNote).HasMaxLength(500);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Variant)
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
