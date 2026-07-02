using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("PaymentAllocations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AppliedAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.InvoicePayment)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.InvoicePaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SaleInvoice)
            .WithMany()
            .HasForeignKey(x => x.SaleInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Purchase)
            .WithMany()
            .HasForeignKey(x => x.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.InvoicePaymentId });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.SaleInvoiceId });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.PurchaseId });
    }
}
