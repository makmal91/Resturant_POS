using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("idx_country_code");

        builder.HasMany(c => c.Cities)
            .WithOne(city => city.Country)
            .HasForeignKey(city => city.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
