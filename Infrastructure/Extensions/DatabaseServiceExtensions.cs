using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure;

public static class DatabaseServiceExtensions
{
    public const string DefaultConnectionName = "DefaultConnection";

    public static string GetRequiredConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DefaultConnectionName}' is not configured in appsettings.json.");
        }

        return connectionString;
    }

    public static IServiceCollection AddPosDatabase(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        services.AddDbContext<POSDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        return services;
    }
}
