using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class GlTransactionConfiguration : IEntityTypeConfiguration<GlTransaction>
{
    public void Configure(EntityTypeBuilder<GlTransaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.BranchId).IsRequired();
        builder.Property(x => x.DebitAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreditAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TransactionType).HasConversion<int>().IsRequired();
        builder.Property(x => x.GroupId).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.IsReversal).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.OriginalGroupId);
        builder.Property(x => x.ReversalOfGroupId);

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("idx_transactions_accountid");
        builder.HasIndex(x => x.BranchId)
            .HasDatabaseName("idx_transactions_branchid");
        builder.HasIndex(x => new { x.BranchId, x.AccountId, x.IsActive })
            .HasDatabaseName("idx_transactions_branch_account_active");
        builder.HasIndex(x => x.Date)
            .HasDatabaseName("idx_transactions_date");
        builder.HasIndex(x => x.GroupId)
            .HasDatabaseName("idx_transactions_groupid");
    }
}
