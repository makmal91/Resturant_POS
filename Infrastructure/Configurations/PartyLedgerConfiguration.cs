using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class CustomerLedgerTransactionConfiguration : IEntityTypeConfiguration<CustomerLedgerTransaction>
{
    public void Configure(EntityTypeBuilder<CustomerLedgerTransaction> builder)
    {
        builder.ToTable("CustomerLedgerTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Debit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Credit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RunningBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.CustomerId, x.Date });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.ReferenceId, x.Type });
    }
}

public class SupplierLedgerTransactionConfiguration : IEntityTypeConfiguration<SupplierLedgerTransaction>
{
    public void Configure(EntityTypeBuilder<SupplierLedgerTransaction> builder)
    {
        builder.ToTable("SupplierLedgerTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Debit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Credit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RunningBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.SupplierId, x.Date });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.ReferenceId, x.Type });
    }
}
