using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class PosRegisterConfiguration : IEntityTypeConfiguration<PosRegister>
{
    public void Configure(EntityTypeBuilder<PosRegister> builder)
    {
        builder.ToTable("PosRegisters");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.IsDefault).HasDefaultValue(false);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.LinkedCashAccount)
            .WithMany()
            .HasForeignKey(x => x.LinkedCashAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RegisterSessionConfiguration : IEntityTypeConfiguration<RegisterSession>
{
    public void Configure(EntityTypeBuilder<RegisterSession> builder)
    {
        builder.ToTable("RegisterSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.ExpectedClosing).HasPrecision(18, 2);
        builder.Property(x => x.PhysicalCash).HasPrecision(18, 2);
        builder.Property(x => x.Difference).HasPrecision(18, 2);
        builder.Property(x => x.TotalCashSales).HasPrecision(18, 2);
        builder.Property(x => x.TotalExpensesCash).HasPrecision(18, 2);
        builder.Property(x => x.TotalCashIn).HasPrecision(18, 2);
        builder.Property(x => x.TotalCashOut).HasPrecision(18, 2);
        builder.Property(x => x.TotalAdjustments).HasPrecision(18, 2);
        builder.Property(x => x.OpeningOverrideReason).HasMaxLength(500);
        builder.Property(x => x.CloseMismatchReason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => new { x.PosRegisterId, x.SessionDate });

        builder.HasIndex(x => x.PosRegisterId)
            .IsUnique()
            .HasFilter("[IsClosed] = 0 AND [IsDeleted] = 0");

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.IsClosed });

        builder.HasOne(x => x.PosRegister)
            .WithMany(r => r.Sessions)
            .HasForeignKey(x => x.PosRegisterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
