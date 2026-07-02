using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.ToTable("InvoicePayments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Module).HasConversion<int>();
        builder.Property(x => x.PaymentType).HasConversion<int>();
        builder.Property(x => x.Category)
            .HasColumnName("PaymentCategory")
            .HasMaxLength(20)
            .HasConversion<string>();
        builder.Property(x => x.ReferenceNo).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.IsReversed).HasDefaultValue(false);

        builder.HasOne(x => x.SaleInvoice)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SaleInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Purchase)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.SaleInvoiceId });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.PurchaseId });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.CustomerId, x.PaymentDate });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.SupplierId, x.PaymentDate });
    }
}
