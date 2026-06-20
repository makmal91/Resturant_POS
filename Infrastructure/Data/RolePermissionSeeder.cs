using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Common.Constants;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Data;

public static class RolePermissionSeeder
{
    public static async Task SeedDefaultPermissionsAsync(POSDbContext context, ILogger logger)
    {
        var roles = await context.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        foreach (var role in roles)
        {
            var existingPermissions = await context.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync();

            if (existingPermissions.Count == 0)
            {
                var permissions = BuildDefaultPermissions(role.Name);
                foreach (var permission in permissions)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = role.Id,
                        ModuleName = permission.ModuleName,
                        CanView = permission.CanView,
                        CanCreate = permission.CanCreate,
                        CanEdit = permission.CanEdit,
                        CanDelete = permission.CanDelete,
                        CanExport = permission.CanExport,
                        CanUpload = permission.CanUpload
                    });
                }
            }
            else
            {
                var defaults = BuildDefaultPermissions(role.Name)
                    .ToDictionary(p => p.ModuleName, StringComparer.OrdinalIgnoreCase);

                foreach (var module in PermissionModules.All)
                {
                    if (existingPermissions.Any(p => string.Equals(p.ModuleName, module, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    if (!defaults.TryGetValue(module, out var permission))
                        continue;

                    await context.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = role.Id,
                        ModuleName = permission.ModuleName,
                        CanView = permission.CanView,
                        CanCreate = permission.CanCreate,
                        CanEdit = permission.CanEdit,
                        CanDelete = permission.CanDelete,
                        CanExport = permission.CanExport,
                        CanUpload = permission.CanUpload
                    });
                }
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed permissions for role {RoleName}", role.Name);
            }
        }

        await EnsureCashierDashboardAccessAsync(context, logger);
        await BackfillRolePermissionModuleIdsAsync(context, logger);
    }

    private static async Task BackfillRolePermissionModuleIdsAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var modules = await context.PermissionModules
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.ModuleKey != string.Empty)
                .Select(m => new { m.Id, m.ModuleKey, m.ModuleName })
                .ToListAsync();

            var byKey = modules.ToDictionary(m => m.ModuleKey, m => m.Id, StringComparer.OrdinalIgnoreCase);
            var byName = modules.ToDictionary(m => m.ModuleName, m => m.Id, StringComparer.OrdinalIgnoreCase);

            var permissions = await context.RolePermissions
                .IgnoreQueryFilters()
                .Where(rp => rp.ModuleId == null && !rp.IsDeleted)
                .ToListAsync();

            foreach (var permission in permissions)
            {
                if (byKey.TryGetValue(permission.ModuleName, out var moduleId) ||
                    byName.TryGetValue(permission.ModuleName, out moduleId))
                {
                    permission.ModuleId = moduleId;
                }
            }

            if (permissions.Count > 0)
                await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to backfill RolePermission ModuleId values after seed.");
        }
    }

    private static async Task EnsureCashierDashboardAccessAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var cashierRole = await context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => !r.IsDeleted && r.Name == RoleNames.Cashier);

            if (cashierRole == null)
                return;

            var dashboardModule = await context.PermissionModules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.ModuleKey == PermissionModules.Dashboard);

            var dashboardPerm = await context.RolePermissions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(rp =>
                    rp.RoleId == cashierRole.Id &&
                    (rp.ModuleName == PermissionModules.Dashboard ||
                     (dashboardModule != null && rp.ModuleId == dashboardModule.Id)));

            if (dashboardPerm == null)
            {
                await context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = cashierRole.Id,
                    ModuleId = dashboardModule?.Id,
                    ModuleName = PermissionModules.Dashboard,
                    CanView = true,
                    CanCreate = false,
                    CanEdit = false,
                    CanDelete = false,
                    CanExport = false,
                    CanUpload = false,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                });
            }
            else if (!dashboardPerm.CanView || dashboardPerm.IsDeleted)
            {
                dashboardPerm.CanView = true;
                dashboardPerm.IsDeleted = false;
                dashboardPerm.UpdatedDate = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure cashier dashboard access.");
        }
    }

    private static IReadOnlyList<(string ModuleName, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete, bool CanExport, bool CanUpload)> BuildDefaultPermissions(string roleName)
    {
        if (string.Equals(roleName, RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(roleName, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return PermissionModules.All
                .Select(module => (module, true, true, true, true, true, true))
                .ToList();
        }

        if (string.Equals(roleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            // Admin can manage everything including Role permissions
            // (protected roles like SystemAdmin/SuperAdmin are restricted at the API layer)
            return PermissionModules.All
                .Select(module => (module, true, true, true, true, true, true))
                .ToList();
        }

        if (string.Equals(roleName, RoleNames.Manager, StringComparison.OrdinalIgnoreCase))
        {
            return PermissionModules.All
                .Select(module => module switch
                {
                    PermissionModules.Users or PermissionModules.Roles or PermissionModules.Businesses =>
                        (module, false, false, false, false, false, false),
                    PermissionModules.Reports or PermissionModules.Dashboard or
                    PermissionModules.SalesReports or PermissionModules.ProductWiseSalesReport or PermissionModules.PurchaseReports or
                    PermissionModules.StockReports or PermissionModules.CustomerOutstandingReport or
                    PermissionModules.SupplierPayableReport or PermissionModules.ProfitLossReport or
                    PermissionModules.CustomerReceivableAgingReport or PermissionModules.SupplierPayableAgingReport =>
                        (module, true, false, false, false, true, false),
                    PermissionModules.PosBilling =>
                        (module, true, false, false, false, false, false),
                    PermissionModules.Expenses or PermissionModules.CashFlow or PermissionModules.PartyLedger =>
                        (module, true, true, false, false, true, false),
                    _ => (module, true, true, true, false, true, true)
                })
                .ToList();
        }

        if (string.Equals(roleName, RoleNames.Cashier, StringComparison.OrdinalIgnoreCase))
        {
            return PermissionModules.All
                .Select(module =>
                {
                    if (string.Equals(module, PermissionModules.Dashboard, StringComparison.OrdinalIgnoreCase))
                        return (module, true, false, false, false, false, false);

                    if (string.Equals(module, PermissionModules.PosBilling, StringComparison.OrdinalIgnoreCase))
                        return (module, true, true, false, false, false, false);

                    if (string.Equals(module, PermissionModules.Orders, StringComparison.OrdinalIgnoreCase))
                        return (module, true, true, false, false, false, false);

                    return (module, false, false, false, false, false, false);
                })
                .ToList();
        }

        return PermissionModules.All
            .Select(module => (module, false, false, false, false, false, false))
            .ToList();
    }
}
