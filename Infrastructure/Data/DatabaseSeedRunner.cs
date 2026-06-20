using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class DatabaseSeedRunner
{
    public static async Task<SeedVerificationReport> RunAsync(POSDbContext context, ILogger logger)
    {
        logger.LogInformation("Running database seed...");

        await TenantSeedDatabaseInitializer.EnsureSeedDataAsync(context, logger);

        await PermissionModuleSeeder.SeedDefaultModulesAsync(context, logger);

        await NavigationMenuDatabaseInitializer.EnsureSchemaAsync(context, logger);
        await NavigationMenuSeeder.SeedDefaultMenusAsync(context, logger);

        await UnitMasterDatabaseInitializer.SeedDefaultUnitsAsync(context, logger);
        await VariantMasterDatabaseInitializer.SeedDefaultSizesAndColorsAsync(context, logger);
        await MasterDataDatabaseInitializer.SeedReferenceDataAsync(context, logger);
        await CustomerInitializer.SeedWalkInCustomersAsync(context, logger);
        await RolePermissionSeeder.SeedDefaultPermissionsAsync(context, logger);

        var report = await DatabaseSeedVerifier.VerifyAsync(context, logger);

        if (report.IsComplete)
            logger.LogInformation("Database seed completed successfully.");
        else
            logger.LogWarning(
                "Database seed finished with {IssueCount} issue(s). Re-run seed or check startup logs.",
                report.Warnings.Count);

        return report;
    }
}
