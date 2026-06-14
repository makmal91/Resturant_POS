using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.CountryId)
            .HasDatabaseName("idx_city_countryid");

        builder.HasIndex(c => new { c.CountryId, c.Name })
            .IsUnique()
            .HasDatabaseName("idx_city_country_name");
    }
}
