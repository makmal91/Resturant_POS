using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Data;

public static class UserManagementDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[UserBranches]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[UserBranches] (
                    [UserId] INT NOT NULL,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [PK_UserBranches] PRIMARY KEY ([UserId], [BranchId]),
                    CONSTRAINT [FK_UserBranches_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_UserBranches_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id])
                );
                CREATE INDEX [idx_userbranch_branchid] ON [UserBranches]([BranchId]);
                CREATE INDEX [idx_userbranch_userid] ON [UserBranches]([UserId]);
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[RolePermissions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[RolePermissions] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [RoleId] INT NOT NULL,
                    [ModuleName] NVARCHAR(100) NOT NULL,
                    [CanView] BIT NOT NULL DEFAULT 0,
                    [CanCreate] BIT NOT NULL DEFAULT 0,
                    [CanEdit] BIT NOT NULL DEFAULT 0,
                    [CanDelete] BIT NOT NULL DEFAULT 0,
                    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [idx_rolepermission_role_module] ON [RolePermissions]([RoleId], [ModuleName]);
            END
            """,
            """
            IF COL_LENGTH('Users', 'IsActive') IS NULL
                ALTER TABLE [Users] ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT 1;
            IF COL_LENGTH('Users', 'DeletedAt') IS NULL
                ALTER TABLE [Users] ADD [DeletedAt] DATETIME2 NULL;
            IF COL_LENGTH('Roles', 'Description') IS NULL
                ALTER TABLE [Roles] ADD [Description] NVARCHAR(500) NOT NULL CONSTRAINT [DF_Roles_Description] DEFAULT '';
            IF COL_LENGTH('Roles', 'IsActive') IS NULL
                ALTER TABLE [Roles] ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_Roles_IsActive] DEFAULT 1;
            IF COL_LENGTH('RolePermissions', 'CanExport') IS NULL
                ALTER TABLE [RolePermissions] ADD [CanExport] BIT NOT NULL CONSTRAINT [DF_RolePermissions_CanExport] DEFAULT 0;
            IF COL_LENGTH('RolePermissions', 'CanUpload') IS NULL
                ALTER TABLE [RolePermissions] ADD [CanUpload] BIT NOT NULL CONSTRAINT [DF_RolePermissions_CanUpload] DEFAULT 0;
            """,
            """
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_role_branchid_name' AND object_id = OBJECT_ID('Roles'))
                DROP INDEX [idx_role_branchid_name] ON [Roles];
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_role_businessid_name' AND object_id = OBJECT_ID('Roles'))
                DROP INDEX [idx_role_businessid_name] ON [Roles];
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_role_name' AND object_id = OBJECT_ID('Roles'))
                CREATE UNIQUE INDEX [idx_role_name] ON [Roles]([Name]);
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM [UserBranches] WHERE [UserId] = 1 AND [BranchId] = 1)
                INSERT INTO [UserBranches] ([UserId], [BranchId]) VALUES (1, 1);
            UPDATE [Users] SET [IsActive] = 1 WHERE [IsActive] = 0 AND [Status] = 0;
            """
        };

        foreach (var batch in batches)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(batch);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "User management schema batch skipped or partially applied.");
            }
        }

        await SeedRolesAsync(context, logger);
        await SeedAdminUserAsync(context, logger);
    }

    private static async Task SeedRolesAsync(POSDbContext context, ILogger logger)
    {
        var seedRoles = new (string Name, string Description, string Permissions)[]
        {
            ("System Admin", "Full system access", "all"),
            ("Super Admin", "All branches access", "all"),
            ("Admin", "Branch-level management", "branch_admin"),
            ("Manager", "Operations control", "operations"),
            ("Cashier", "POS billing access", "pos")
        };

        foreach (var role in seedRoles)
        {
            try
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = {role.Name})
                        INSERT INTO [Roles] ([Name], [Description], [Permissions], [IsActive], [CreatedDate], [IsDeleted])
                        VALUES ({role.Name}, {role.Description}, {role.Permissions}, 1, GETUTCDATE(), 0);
                    """);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed role {RoleName}", role.Name);
            }
        }
    }

    private static async Task SeedAdminUserAsync(POSDbContext context, ILogger logger)
    {
        const string adminUsername = "admin";
        const string defaultPassword = "Admin@123";

        try
        {
            var adminExists = await context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Username.ToLower() == adminUsername && !u.IsDeleted);

            if (adminExists)
                return;

            var systemAdminRoleId = await context.Roles
                .Where(r => r.Name == RoleNames.SystemAdmin && r.IsActive)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (systemAdminRoleId == 0)
            {
                logger.LogWarning("Default admin user was not seeded because the System Admin role is missing.");
                return;
            }

            var businessId = await context.Businesses
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();

            if (businessId == 0)
            {
                logger.LogWarning("Default admin user was not seeded because no business exists.");
                return;
            }

            var branchId = await context.Branches
                .Where(b => b.BusinessId == businessId && b.IsActive)
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [Users] (
                    [FullName], [Username], [PasswordHash], [Phone], [Email], [RoleId], [BusinessId], [BranchId],
                    [IsActive], [Salary], [ShiftType], [Status], [CreatedDate], [IsDeleted])
                VALUES (
                    {"System Administrator"}, {adminUsername}, {passwordHash}, {"+1234567890"}, {"admin@restaurant.com"},
                    {systemAdminRoleId}, {businessId}, {branchId}, {true}, {0m}, {(int)ShiftType.Flexible},
                    {(int)UserStatus.Active}, {DateTime.UtcNow}, {false});
                """);

            var adminUserId = await context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Username.ToLower() == adminUsername && !u.IsDeleted)
                .Select(u => u.Id)
                .FirstAsync();

            if (branchId > 0)
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    IF NOT EXISTS (SELECT 1 FROM [UserBranches] WHERE [UserId] = {adminUserId} AND [BranchId] = {branchId})
                        INSERT INTO [UserBranches] ([UserId], [BranchId]) VALUES ({adminUserId}, {branchId});
                    """);
            }

            logger.LogInformation("Seeded default admin user '{AdminUsername}'.", adminUsername);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed default admin user.");
        }
    }
}
