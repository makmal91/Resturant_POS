using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace POSSystem.Infrastructure.Data;

public static class DatabaseBootstrapper
{
    public static async Task InitializeAsync(
        POSDbContext context,
        IConfiguration configuration,
        ILogger logger,
        string[]? commandLineArgs = null)
    {
        var options = DatabaseBootstrapOptions.FromConfiguration(configuration, commandLineArgs);
        var databaseName = GetDatabaseName(configuration.GetRequiredConnectionString());

        logger.LogInformation(
            "Database startup options: CreateDatabase={CreateDatabase}, Migrations={ApplyMigrations}, SchemaPatches={ApplySchemaPatches}, Seed={RunSeed}",
            options.CreateDatabaseOnStartup,
            options.ApplyMigrationsOnStartup,
            options.ApplySchemaPatchesOnStartup,
            options.RunSeedOnStartup);

        await EnsureDatabaseExistsAsync(configuration, logger, options.CreateDatabaseOnStartup);

        if (!await context.Database.CanConnectAsync())
            throw BuildConnectionFailedException(databaseName);

        if (options.ApplyMigrationsOnStartup)
        {
            try
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied.");
            }
            catch (SqlException ex) when (ex.Number == 262)
            {
                logger.LogWarning(
                    "Database migration skipped: login lacks CREATE DATABASE permission on master. " +
                    "Apply migrations once using an admin account, or grant the app pool db_owner on '{DatabaseName}'.",
                    databaseName);
            }
            catch (Exception ex)
            {
                await BootstrapExceptionLogger.LogAsync(context, logger, ex, "DatabaseBootstrap", actionName: "Migrate");
            }
        }
        else
        {
            logger.LogInformation(
                "Database migrations skipped. Set Database:ApplyMigrationsOnStartup to true, pass --apply-migrations, " +
                "or run: dotnet ef database update --project Infrastructure --startup-project API");
        }

        if (!await context.Database.CanConnectAsync())
            throw BuildConnectionFailedException(databaseName);

        if (options.ApplySchemaPatchesOnStartup)
        {
            await RunSchemaPatchesAsync(context, logger);
        }
        else
        {
            logger.LogInformation(
                "Schema patches skipped. Set Database:ApplySchemaPatchesOnStartup to true, pass --apply-schema-patches, " +
                "or call DatabaseBootstrapper.RunSchemaPatchesAsync when upgrading schema.");
        }

        if (options.RunSeedOnStartup)
        {
            var report = await RunSeedAsync(context, logger);
            if (!report.IsComplete)
            {
                logger.LogWarning(
                    "Startup seed completed with {IssueCount} issue(s). Call GET /api/database/seed-status or POST /api/database/seed after deploy.",
                    report.Warnings.Count);
            }
        }
        else
        {
            logger.LogInformation(
                "Database seed skipped. Set Database:RunSeedOnStartup to true, pass --seed-database, " +
                "or call DatabaseBootstrapper.RunSeedAsync when you need reference data.");
        }
    }

    public static async Task<SeedVerificationReport> RunSeedAsync(POSDbContext context, ILogger logger) =>
        await DatabaseSeedRunner.RunAsync(context, logger);

    public static Task RunSchemaPatchesAsync(POSDbContext context, ILogger logger) =>
        ApplySchemaPatchesAsync(context, logger);

    private static async Task ApplySchemaPatchesAsync(POSDbContext context, ILogger logger)
    {
        var modules = new (string Name, Func<POSDbContext, ILogger, Task> Apply)[]
        {
            ("Core", CoreDatabaseInitializer.EnsureSchemaAsync),
            ("UserManagement", UserManagementDatabaseInitializer.EnsureSchemaAsync),
            ("PermissionModule", PermissionModuleDatabaseInitializer.EnsureSchemaAsync),
            ("NavigationMenu", NavigationMenuDatabaseInitializer.EnsureSchemaAsync),
            ("UnitMaster", UnitMasterDatabaseInitializer.EnsureSchemaAsync),
            ("VariantMaster", VariantMasterDatabaseInitializer.EnsureSchemaAsync),
            ("Brand", BrandDatabaseInitializer.EnsureSchemaAsync),
            ("ProductManagement", ProductManagementDatabaseInitializer.EnsureSchemaAsync),
            ("OpeningStock", OpeningStockDatabaseInitializer.EnsureSchemaAsync),
            ("StockTransfer", StockTransferDatabaseInitializer.EnsureSchemaAsync),
            ("PurchaseWarehouse", PurchaseWarehouseInitializer.EnsureSchemaAsync),
            ("SaleInvoice", SaleInvoiceInitializer.EnsureSchemaAsync),
            ("Customer", CustomerInitializer.EnsureSchemaAsync),
            ("CodeSequence", CodeSequenceDatabaseInitializer.EnsureSchemaAsync),
            ("CashFlow", CashFlowDatabaseInitializer.EnsureSchemaAsync),
            ("PartyLedger", PartyLedgerInitializer.EnsureSchemaAsync),
            ("InvoicePayment", InvoicePaymentInitializer.EnsureSchemaAsync),
            ("Accounting", AccountingDatabaseInitializer.EnsureSchemaAsync),
            ("PosRegister", async (ctx, log) =>
            {
                await PosRegisterDatabaseInitializer.EnsureSchemaAsync(ctx, log);
                await PosRegisterDatabaseInitializer.SeedDefaultRegistersAsync(ctx, log);
            }),
            ("MasterData", MasterDataDatabaseInitializer.EnsureSchemaAsync),
        };

        logger.LogInformation("Applying runtime schema patches ({ModuleCount} modules)...", modules.Length);

        var failures = 0;
        foreach (var (name, apply) in modules)
        {
            try
            {
                await apply(context, logger);
            }
            catch (Exception ex)
            {
                failures++;
                logger.LogError(ex, "Schema patch module '{Module}' failed.", name);
            }
        }

        if (failures > 0)
        {
            logger.LogError(
                "Schema patches finished with {FailureCount} module failure(s). " +
                "Startup DB errors are written to the console log, not the ExceptionLogs table.",
                failures);
        }
        else
        {
            logger.LogInformation("Schema patches completed successfully.");
        }
    }

    private static async Task EnsureDatabaseExistsAsync(IConfiguration configuration, ILogger logger, bool createIfMissing)
    {
        var connectionString = configuration.GetRequiredConnectionString();
        var databaseName = GetDatabaseName(connectionString);

        if (!createIfMissing)
        {
            if (await DatabaseExistsOnServerAsync(BuildMasterConnectionString(connectionString).ConnectionString, databaseName))
            {
                logger.LogInformation("Database '{DatabaseName}' is available.", databaseName);
                return;
            }

            throw new InvalidOperationException(
                $"Database '{databaseName}' does not exist. Create it manually in SQL Server, " +
                "or set Database:CreateDatabaseOnStartup to true / start with --create-database.");
        }

        var builder = BuildMasterConnectionString(connectionString);
        if (builder.ConnectTimeout < 60)
            builder.ConnectTimeout = 60;

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();

                if (await DatabaseExistsOnServerAsync(connection, databaseName))
                {
                    logger.LogInformation("Database '{DatabaseName}' is available.", databaseName);
                    return;
                }

                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{databaseName.Replace("]", "]]")}];";
                await command.ExecuteNonQueryAsync();
                logger.LogInformation("Database '{DatabaseName}' created.", databaseName);
                return;
            }
            catch (SqlException ex) when (ex.Number == 262)
            {
                if (await DatabaseExistsOnServerAsync(builder.ConnectionString, databaseName))
                {
                    logger.LogInformation(
                        "Database '{DatabaseName}' exists; skipping create (login lacks CREATE DATABASE on master).",
                        databaseName);
                    return;
                }

                throw new InvalidOperationException(
                    $"Cannot create database '{databaseName}'. Create it manually in SQL Server Management Studio, " +
                    "then grant the IIS app pool identity access.",
                    ex);
            }
            catch (SqlException ex) when (attempt < maxAttempts && IsTransientConnectionError(ex))
            {
                logger.LogWarning(ex, "SQL connection attempt {Attempt}/{MaxAttempts} failed. Retrying...", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
        }
    }

    private static SqlConnectionStringBuilder BuildMasterConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };
        return builder;
    }

    private static string GetDatabaseName(string connectionString)
    {
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Database name is missing from the connection string.");

        return databaseName;
    }

    private static async Task<bool> DatabaseExistsOnServerAsync(string masterConnectionString, string databaseName)
    {
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();
        return await DatabaseExistsOnServerAsync(connection, databaseName);
    }

    private static async Task<bool> DatabaseExistsOnServerAsync(SqlConnection connection, string databaseName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sys.databases WHERE [name] = @name";
        command.Parameters.AddWithValue("@name", databaseName);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private static InvalidOperationException BuildConnectionFailedException(string databaseName) =>
        new(
            $"Unable to connect to database '{databaseName}'. " +
            "When hosting under IIS, the app pool Windows identity needs a SQL login. " +
            "In SQL Server Management Studio run (replace AppPoolName with your IIS app pool name):\n" +
            "CREATE LOGIN [IIS AppPool\\AppPoolName] FROM WINDOWS;\n" +
            $"USE [{databaseName}];\n" +
            "CREATE USER [IIS AppPool\\AppPoolName] FOR LOGIN [IIS AppPool\\AppPoolName];\n" +
            "ALTER ROLE db_owner ADD MEMBER [IIS AppPool\\AppPoolName];");

    private static bool IsTransientConnectionError(SqlException ex) =>
        ex.Number is -2 or 233 or 4060 or 10054 or 10060;
}
