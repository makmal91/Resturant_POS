namespace POSSystem.Domain;

public static class RoleNames
{
    public const string SystemAdmin = "System Admin";
    public const string SuperAdmin = "Super Admin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";

    public static readonly IReadOnlyList<string> All =
    [
        SystemAdmin,
        SuperAdmin,
        Admin,
        Manager,
        Cashier
    ];

    public static bool IsMasterUser(string roleName) =>
        string.Equals(roleName, SystemAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool IsSuperAdmin(string roleName) =>
        string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool IsProtectedRole(string roleName) =>
        IsMasterUser(roleName) || IsSuperAdmin(roleName);

    /// <summary>
    /// Only System Admin bypasses permission checks. Super Admin is limited to assigned permissions.
    /// </summary>
    public static bool CanBypassPermissions(string roleName) =>
        IsMasterUser(roleName);

    public static bool HasGlobalBranchAccess(string roleName) =>
        IsMasterUser(roleName) || IsSuperAdmin(roleName);

    public static bool IsGlobalRole(string roleName) =>
        HasGlobalBranchAccess(roleName);

    public static bool BypassesBranchRequirement(string roleName) =>
        HasGlobalBranchAccess(roleName);
}
