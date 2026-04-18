using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Salary)
            .HasPrecision(10, 2);

        // Indexes
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("idx_user_username");

        builder.HasIndex(u => u.Phone)
            .HasDatabaseName("idx_user_phone");

        builder.HasIndex(u => u.Email)
            .HasDatabaseName("idx_user_email");

        builder.HasIndex(u => u.BranchId)
            .HasDatabaseName("idx_user_branchid");

        builder.HasIndex(u => u.RoleId)
            .HasDatabaseName("idx_user_roleid");

        // Relationships
        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AssignedOrders)
            .WithOne(o => o.Waiter)
            .HasForeignKey(o => o.WaiterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
