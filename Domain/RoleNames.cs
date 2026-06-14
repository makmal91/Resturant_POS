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

    public static bool IsGlobalRole(string roleName) =>
        IsMasterUser(roleName) ||
        string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool BypassesBranchRequirement(string roleName) =>
        IsMasterUser(roleName);
}
