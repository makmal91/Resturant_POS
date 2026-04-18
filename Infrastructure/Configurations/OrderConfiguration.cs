using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Notes)
            .HasMaxLength(500);

        builder.Property(o => o.Subtotal)
            .HasPrecision(10, 2);

        builder.Property(o => o.Discount)
            .HasPrecision(10, 2);

        builder.Property(o => o.Tax)
            .HasPrecision(10, 2);

        builder.Property(o => o.TotalAmount)
            .HasPrecision(10, 2);

        // Indexes
        builder.HasIndex(o => o.BranchId)
            .HasDatabaseName("idx_order_branchid");

        builder.HasIndex(o => o.TableId)
            .HasDatabaseName("idx_order_tableid");

        builder.HasIndex(o => o.CustomerId)
            .HasDatabaseName("idx_order_customerid");

        builder.HasIndex(o => o.WaiterId)
            .HasDatabaseName("idx_order_waiterid");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("idx_order_status");

        builder.HasIndex(o => new { o.BranchId, o.CreatedDate })
            .HasDatabaseName("idx_order_branchid_createddate");

        // Relationships
        builder.HasOne(o => o.Table)
            .WithMany(t => t.Orders)
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Waiter)
            .WithMany(u => u.AssignedOrders)
            .HasForeignKey(o => o.WaiterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(o => o.Payments)
            .WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
