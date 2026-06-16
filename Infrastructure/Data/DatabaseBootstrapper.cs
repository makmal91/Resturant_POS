using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace POSSystem.Infrastructure.Data;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(POSDbContext context, IConfiguration configuration, ILogger logger)
    {
        await EnsureDatabaseExistsAsync(configuration, logger);

        try
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database migration failed. Continuing with runtime schema initializers.");
        }

        if (!await context.Database.CanConnectAsync())
            throw new InvalidOperationException("Unable to connect to the database after initialization.");

        await CoreDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await UserManagementDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await PermissionModuleDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await NavigationMenuDatabaseInitializer.EnsureSchemaAsync(context, logger);

        await TenantSeedDatabaseInitializer.EnsureSeedDataAsync(context, logger);

        await UnitMasterDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await BrandDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await ProductManagementDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await PurchaseWarehouseInitializer.EnsureSchemaAsync(context, logger);
        await SaleInvoiceInitializer.EnsureSchemaAsync(context, logger);
        await CustomerInitializer.EnsureSchemaAsync(context, logger);
        await CodeSequenceDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await CashFlowDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await MasterDataDatabaseInitializer.EnsureSchemaAsync(context, logger);

        await RolePermissionSeeder.SeedDefaultPermissionsAsync(context, logger);
    }

    private static async Task EnsureDatabaseExistsAsync(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Database name is missing from the connection string.");

        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE [name] = N'{databaseName.Replace("'", "''")}')
                CREATE DATABASE [{databaseName.Replace("]", "]]")}];
            """;

        await command.ExecuteNonQueryAsync();
        logger.LogInformation("Database '{DatabaseName}' is available.", databaseName);
    }
}
