using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.HasKey(ub => new { ub.UserId, ub.BranchId });

        builder.HasIndex(ub => ub.BranchId)
            .HasDatabaseName("idx_userbranch_branchid");

        builder.HasIndex(ub => ub.UserId)
            .HasDatabaseName("idx_userbranch_userid");

        builder.HasOne(ub => ub.User)
            .WithMany(u => u.UserBranches)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ub => ub.Branch)
            .WithMany()
            .HasForeignKey(ub => ub.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
