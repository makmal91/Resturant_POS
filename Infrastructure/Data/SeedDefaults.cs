namespace POSSystem.Infrastructure.Data;

internal static class SeedDefaults
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "Admin@123";

    /// <summary>Usernames treated as the seeded system administrator account.</summary>
    public static readonly string[] SeedAdminUsernames = [AdminUsername, "makmal"];

    /// <summary>BCrypt hash of <see cref="AdminPassword"/>.</summary>
    public const string AdminPasswordHash = "$2a$11$W7Mi6nl3DiHePG3yDxDRv.VuEY5uE2Jfa2VizDS.h78g1bjFEYuuu";

    /// <summary>Fixed id of the primary seeded admin user.</summary>
    public const int SeedUserId = 1;
}
