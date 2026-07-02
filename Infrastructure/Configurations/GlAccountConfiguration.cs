using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class GlAccountConfiguration : IEntityTypeConfiguration<GlAccount>
{
    public void Configure(EntityTypeBuilder<GlAccount> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("CreatedDate").IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.Property(x => x.ModifiedAt).HasColumnName("UpdatedDate");

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("idx_accounts_name")
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("idx_accounts_parentid");
    }
}
