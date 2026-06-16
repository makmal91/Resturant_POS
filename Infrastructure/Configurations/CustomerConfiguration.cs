using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode).HasMaxLength(50);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.CNIC).HasMaxLength(20);
        builder.Property(c => c.CustomerType).HasConversion<int>();
        builder.Property(c => c.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(c => c.CreditLimit).HasColumnType("decimal(18,2)");

        builder.HasIndex(c => new { c.BusinessId, c.BranchId, c.Phone })
            .HasFilter("[Phone] IS NOT NULL AND [IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("idx_customer_branch_phone_unique");

        builder.HasIndex(c => c.BranchId)
            .HasDatabaseName("idx_customer_branchid");

        builder.HasIndex(c => new { c.BusinessId, c.BranchId, c.IsWalkIn })
            .HasDatabaseName("idx_customer_walkin");

        builder.HasIndex(c => new { c.BusinessId, c.BranchId, c.CustomerCode })
            .IsUnique()
            .HasFilter("[CustomerCode] <> '' AND [IsDeleted] = 0")
            .HasDatabaseName("idx_customer_branch_code");

        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
