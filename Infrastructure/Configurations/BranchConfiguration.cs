using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.Ignore(b => b.BranchId);
        builder.Ignore(b => b.CreatedByName);
        builder.Ignore(b => b.ModifiedByName);
        builder.Ignore(b => b.Business);
        builder.Ignore(b => b.Country);
        builder.Ignore(b => b.City);

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(b => b.Code)
            .IsUnique()
            .HasDatabaseName("idx_branch_code");

        builder.HasIndex(b => b.Phone)
            .HasDatabaseName("idx_branch_phone");

        builder.HasIndex(b => b.Email)
            .HasDatabaseName("idx_branch_email");

        builder.HasIndex(b => b.BusinessId)
            .HasDatabaseName("idx_branch_businessid");

        builder.HasIndex(b => b.CountryId)
            .HasDatabaseName("idx_branch_countryid");

        builder.HasIndex(b => b.CityId)
            .HasDatabaseName("idx_branch_cityid");

        builder.HasMany(b => b.Users)
            .WithOne(u => u.Branch)
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Roles)
            .WithOne(r => r.Branch)
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.MenuCategories)
            .WithOne(mc => mc.Branch)
            .HasForeignKey(mc => mc.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.MenuItems)
            .WithOne(mi => mi.Branch)
            .HasForeignKey(mi => mi.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Tables)
            .WithOne(t => t.Branch)
            .HasForeignKey(t => t.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Customers)
            .WithOne(c => c.Branch)
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
