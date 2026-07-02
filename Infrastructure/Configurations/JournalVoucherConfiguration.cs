using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class JournalVoucherConfiguration : IEntityTypeConfiguration<JournalVoucher>
{
    public void Configure(EntityTypeBuilder<JournalVoucher> builder)
    {
        builder.ToTable("JournalVouchers");
        builder.HasIndex(v => new { v.BusinessId, v.BranchId, v.VoucherDate });
        builder.HasIndex(v => new { v.BusinessId, v.BranchId, v.VoucherNo }).IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(v => v.VoucherNo).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Amount).HasPrecision(18, 2);
        builder.Property(v => v.Description).HasMaxLength(500);

        builder.HasOne(v => v.Branch)
            .WithMany()
            .HasForeignKey(v => v.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
