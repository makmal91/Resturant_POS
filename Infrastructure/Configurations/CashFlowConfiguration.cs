using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class CashFlowTransactionConfiguration : IEntityTypeConfiguration<CashFlowTransaction>
{
    public void Configure(EntityTypeBuilder<CashFlowTransaction> builder)
    {
        builder.ToTable("CashFlowTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionType).HasConversion<int>().IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<int>().IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ReferenceNo).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.TransactionDate).IsRequired();

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.TransactionDate });
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.TransactionType });
    }
}

public class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("CashRegisters");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OpeningCash).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ClosingCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ExpectedCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Difference).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // One register per branch per date
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.RegisterDate })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
