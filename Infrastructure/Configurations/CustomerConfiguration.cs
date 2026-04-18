using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(c => c.BranchId)
            .HasDatabaseName("idx_customer_branchid");

        builder.HasIndex(c => c.Phone)
            .HasDatabaseName("idx_customer_phone");

        builder.HasIndex(c => c.Email)
            .HasDatabaseName("idx_customer_email");

        builder.HasIndex(c => new { c.BranchId, c.Phone })
            .IsUnique()
            .HasDatabaseName("idx_customer_branch_phone");

        // Relationships
        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
