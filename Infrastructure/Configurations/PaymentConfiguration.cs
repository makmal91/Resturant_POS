using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Method)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Amount)
            .HasPrecision(10, 2);

        // Indexes
        builder.HasIndex(p => p.OrderId)
            .HasDatabaseName("idx_payment_orderid");

        builder.HasIndex(p => p.BranchId)
            .HasDatabaseName("idx_payment_branchid");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("idx_payment_status");

        builder.HasIndex(p => new { p.BranchId, p.CreatedDate })
            .HasDatabaseName("idx_payment_branch_createddate");

        // Relationships
        builder.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
