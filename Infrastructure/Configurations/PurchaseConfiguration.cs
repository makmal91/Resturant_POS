using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.InvoiceNo).IsRequired().HasMaxLength(100);
        builder.Property(p => p.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(p => p.Status).HasDefaultValue(PurchaseStatus.Draft);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.PurchaseDate).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(p => p.IsCreditPurchase).HasDefaultValue(false);

        builder.HasIndex(p => new { p.BusinessId, p.BranchId, p.InvoiceNo })
            .IsUnique()
            .HasDatabaseName("idx_purchase_business_branch_invoice");

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Purchases)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Warehouse)
            .WithMany(w => w.Purchases)
            .HasForeignKey(p => p.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Branch)
            .WithMany()
            .HasForeignKey(p => p.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasPrecision(18, 4);
        builder.Property(i => i.ConversionFactor).HasPrecision(18, 4).HasDefaultValue(1);
        builder.Property(i => i.BaseQuantity).HasPrecision(18, 4);
        builder.Property(i => i.CostPrice).HasPrecision(18, 2);
        builder.Property(i => i.TotalCost).HasPrecision(18, 2);

        builder.HasOne(i => i.Purchase)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Variant)
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Unit)
            .WithMany()
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Branch)
            .WithMany()
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
