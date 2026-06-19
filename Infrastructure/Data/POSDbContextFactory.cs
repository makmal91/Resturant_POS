using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace POSSystem.Infrastructure.Data;

/// <summary>
/// Enables EF Core CLI tools to resolve the connection string from appsettings.json
/// without hardcoding a database name in code.
/// </summary>
public class POSDbContextFactory : IDesignTimeDbContextFactory<POSDbContext>
{
    public POSDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetRequiredConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<POSDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new POSDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }

    private static IConfiguration BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = ResolveAppSettingsPath();

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveAppSettingsPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
            return currentDirectory;

        var apiProjectPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "API"));
        if (File.Exists(Path.Combine(apiProjectPath, "appsettings.json")))
            return apiProjectPath;

        throw new InvalidOperationException(
            "Could not locate appsettings.json. Run EF commands with --startup-project API.");
    }
}
