using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Common.Constants;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Data;

public sealed record SeedVerificationReport
{
    public required bool IsComplete { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
    public required SeedCounts Counts { get; init; }
    public required IReadOnlyList<string> MissingModuleKeys { get; init; }
}

public sealed record SeedCounts
{
    public int ModulesActive { get; init; }
    public int ModulesWithKeyActive { get; init; }
    public int ExpectedModules { get; init; }
    public int ExpectedModulesWithKey { get; init; }
    public int ModuleForms { get; init; }
    public int Roles { get; init; }
    public int Users { get; init; }
    public int RolePermissions { get; init; }
    public int MenusActive { get; init; }
    public int Businesses { get; init; }
    public int Branches { get; init; }
}

public static class DatabaseSeedVerifier
{
    public static async Task<SeedVerificationReport> VerifyAsync(POSDbContext context, ILogger logger)
    {
        var expectedModules = PermissionModuleSeeder.ExpectedModuleCount;
        var expectedKeys = PermissionModuleSeeder.ExpectedModuleKeys;

        var modulesActive = await context.PermissionModules
            .IgnoreQueryFilters()
            .CountAsync(m => !m.IsDeleted && m.IsActive);

        var modulesWithKey = await context.PermissionModules
            .IgnoreQueryFilters()
            .Where(m => !m.IsDeleted && m.IsActive && m.ModuleKey != string.Empty)
            .Select(m => m.ModuleKey)
            .ToListAsync();

        var existingKeys = new HashSet<string>(modulesWithKey, StringComparer.OrdinalIgnoreCase);
        var missingKeys = expectedKeys
            .Where(k => !existingKeys.Contains(k))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var counts = new SeedCounts
        {
            ModulesActive = modulesActive,
            ModulesWithKeyActive = modulesWithKey.Count,
            ExpectedModules = expectedModules,
            ExpectedModulesWithKey = expectedKeys.Count,
            ModuleForms = await context.ModuleForms.IgnoreQueryFilters().CountAsync(f => !f.IsDeleted && f.IsActive),
            Roles = await context.Roles.IgnoreQueryFilters().CountAsync(r => !r.IsDeleted),
            Users = await context.Users.IgnoreQueryFilters().CountAsync(u => !u.IsDeleted),
            RolePermissions = await context.RolePermissions.IgnoreQueryFilters().CountAsync(rp => !rp.IsDeleted),
            MenusActive = await TableExistsAsync(context, "Menus")
                ? await context.Menus.IgnoreQueryFilters().CountAsync(m => m.IsActive)
                : 0,
            Businesses = await context.Businesses.IgnoreQueryFilters().CountAsync(b => !b.IsDeleted),
            Branches = await context.Branches.IgnoreQueryFilters().CountAsync(b => !b.IsDeleted)
        };

        var warnings = new List<string>();

        if (modulesActive < expectedModules)
            warnings.Add($"Modules: expected at least {expectedModules} active rows, found {modulesActive}.");

        if (missingKeys.Count > 0)
            warnings.Add($"Missing module keys ({missingKeys.Count}): {string.Join(", ", missingKeys.Take(10))}" +
                         (missingKeys.Count > 10 ? ", ..." : string.Empty));

        if (counts.Roles < 5)
            warnings.Add($"Roles: expected 5 default roles, found {counts.Roles}.");

        if (counts.Users < 1)
            warnings.Add("Users: no active users found (admin user missing).");

        if (counts.Businesses < 1 || counts.Branches < 1)
            warnings.Add("Business/branch seed incomplete.");

        var adminRole = await context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.Name == RoleNames.SystemAdmin);

        if (adminRole != null)
        {
            var adminPermCount = await context.RolePermissions
                .IgnoreQueryFilters()
                .CountAsync(rp => !rp.IsDeleted && rp.RoleId == adminRole.Id);

            if (adminPermCount < PermissionModules.All.Count)
            {
                warnings.Add(
                    $"System Admin permissions: expected at least {PermissionModules.All.Count}, found {adminPermCount}.");
            }
        }

        if (counts.ModuleForms < expectedKeys.Count)
        {
            warnings.Add(
                $"Module forms: expected about {expectedKeys.Count}, found {counts.ModuleForms}.");
        }

        var isComplete = warnings.Count == 0;

        if (isComplete)
            logger.LogInformation("Database seed verification passed.");
        else
            logger.LogWarning("Database seed verification found {IssueCount} issue(s).", warnings.Count);

        foreach (var warning in warnings)
            logger.LogWarning("{SeedWarning}", warning);

        return new SeedVerificationReport
        {
            IsComplete = isComplete,
            Warnings = warnings,
            Counts = counts,
            MissingModuleKeys = missingKeys
        };
    }

    private static async Task<bool> TableExistsAsync(POSDbContext context, string tableName)
    {
        var exists = await context.Database
            .SqlQueryRaw<int>($"SELECT CASE WHEN OBJECT_ID(N'dbo.{tableName}', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]")
            .FirstOrDefaultAsync();
        return exists == 1;
    }
}
