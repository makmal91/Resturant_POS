namespace POSSystem.Domain;

public static class RoleProtection
{
    public static void EnsureCanManageUser(string? actorRoleName, string targetUserRoleName)
    {
        if (!RoleNames.IsProtectedRole(targetUserRoleName))
            return;

        if (RoleNames.IsMasterUser(actorRoleName ?? string.Empty))
            return;

        throw new InvalidOperationException(
            $"You do not have permission to manage {targetUserRoleName} accounts.");
    }

    public static void EnsureCanAssignRole(string? actorRoleName, string roleToAssign)
    {
        if (!RoleNames.IsProtectedRole(roleToAssign))
            return;

        if (RoleNames.IsMasterUser(actorRoleName ?? string.Empty))
            return;

        throw new InvalidOperationException(
            $"You do not have permission to assign the {roleToAssign} role.");
    }

    public static void EnsureCanManageRolePermissions(string? actorRoleName, string targetRoleName)
    {
        if (!RoleNames.IsProtectedRole(targetRoleName))
            return;

        if (RoleNames.IsMasterUser(actorRoleName ?? string.Empty))
            return;

        throw new InvalidOperationException(
            $"You do not have permission to change permissions for {targetRoleName}.");
    }

    public static void EnsureCanDeleteRole(string? actorRoleName, string targetRoleName)
    {
        if (RoleNames.IsMasterUser(targetRoleName))
            throw new InvalidOperationException("The System Admin role cannot be deleted.");

        if (!RoleNames.IsProtectedRole(targetRoleName))
            return;

        if (RoleNames.IsMasterUser(actorRoleName ?? string.Empty))
            return;

        throw new InvalidOperationException(
            $"You do not have permission to delete the {targetRoleName} role.");
    }

    public static void EnsureCanModifyRole(string? actorRoleName, string targetRoleName, string? newRoleName = null)
    {
        EnsureCanManageRolePermissions(actorRoleName, targetRoleName);

        if (RoleNames.IsMasterUser(targetRoleName) &&
            !string.IsNullOrWhiteSpace(newRoleName) &&
            !RoleNames.IsMasterUser(newRoleName))
        {
            throw new InvalidOperationException("The System Admin role name cannot be changed.");
        }
    }
}
