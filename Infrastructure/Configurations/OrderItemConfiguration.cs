using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Quantity)
            .IsRequired();

        builder.Property(oi => oi.Price)
            .HasPrecision(10, 2);

        builder.Property(oi => oi.Discount)
            .HasPrecision(10, 2);

        builder.Property(oi => oi.Total)
            .HasPrecision(10, 2);

        builder.Property(oi => oi.ModifiersJson)
            .HasMaxLength(2000);

        builder.Property(oi => oi.Notes)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("idx_orderitem_orderid");

        builder.HasIndex(oi => oi.MenuItemId)
            .HasDatabaseName("idx_orderitem_menuitemid");

        builder.HasIndex(oi => oi.BranchId)
            .HasDatabaseName("idx_orderitem_branchid");

        // Relationships
        builder.HasOne(oi => oi.MenuItem)
            .WithMany(mi => mi.OrderItems)
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
