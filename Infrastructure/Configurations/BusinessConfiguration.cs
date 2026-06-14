using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.Ignore(b => b.BusinessId);
        builder.Ignore(b => b.BranchId);

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.LegalName)
            .HasMaxLength(250);

        builder.Property(b => b.Logo)
            .HasColumnType("varbinary(max)");

        builder.Property(b => b.LogoFileName)
            .HasMaxLength(255);

        builder.Property(b => b.LogoContentType)
            .HasMaxLength(100);

        builder.Property(b => b.Phone)
            .HasMaxLength(20);

        builder.Property(b => b.Email)
            .HasMaxLength(100);

        builder.Property(b => b.Address)
            .HasMaxLength(500);

        builder.Property(b => b.TaxNumber)
            .HasMaxLength(50);

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(b => b.TimeZone)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(b => b.Name)
            .HasDatabaseName("idx_business_name");

        builder.HasIndex(b => b.Email)
            .HasDatabaseName("idx_business_email");

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
