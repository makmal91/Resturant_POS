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
            return PermissionModules.All
                .Select(module =>
                {
                    var isRolesModule = string.Equals(module, PermissionModules.Roles, StringComparison.OrdinalIgnoreCase);
                    return (module, true, !isRolesModule, !isRolesModule, !isRolesModule, true, true);
                })
                .ToList();
        }

        if (string.Equals(roleName, RoleNames.Manager, StringComparison.OrdinalIgnoreCase))
        {
            return PermissionModules.All
                .Select(module => module switch
                {
                    PermissionModules.Users or PermissionModules.Roles or PermissionModules.Businesses =>
                        (module, false, false, false, false, false, false),
                    PermissionModules.Reports =>
                        (module, true, false, false, false, true, false),
                    PermissionModules.PosBilling =>
                        (module, true, false, false, false, false, false),
                    _ => (module, true, true, true, false, true, true)
                })
                .ToList();
        }

        if (string.Equals(roleName, RoleNames.Cashier, StringComparison.OrdinalIgnoreCase))
        {
            return PermissionModules.All
                .Select(module =>
                {
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
