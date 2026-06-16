using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Data;

public static class TenantSeedDatabaseInitializer
{
    private const int DefaultBusinessId = 1;
    private const int DefaultBranchId = 1;
    private const int DefaultAdminUserId = SeedDefaults.SeedUserId;
    private const int PakistanCountryId = 3;
    private const int KarachiCityId = 5;
    private const string AdminUsername = SeedDefaults.AdminUsername;

    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly (int Id, string Name, string Description, string Permissions)[] SeedRoles =
    [
        (1, RoleNames.SystemAdmin, "Full system access", "all"),
        (2, RoleNames.SuperAdmin, "All branches access", "all"),
        (3, RoleNames.Admin, "Branch-level management", "branch_admin"),
        (4, RoleNames.Manager, "Operations control", "operations"),
        (5, RoleNames.Cashier, "POS billing access", "pos")
    ];

    public static async Task EnsureSeedDataAsync(POSDbContext context, ILogger logger)
    {
        await ExecuteBatchAsync(context, logger, EnsureCurrenciesTableSql());
        await ExecuteBatchAsync(context, logger, SeedCurrenciesSql());
        await ExecuteBatchAsync(context, logger, SeedCountriesSql());
        await ExecuteBatchAsync(context, logger, SeedCitiesSql());
        await ExecuteBatchAsync(context, logger, SeedBusinessSql());
        await ExecuteBatchAsync(context, logger, SeedBranchSql());

        await ExecuteBatchAsync(context, logger, FixLegacyAdminRoleAtIdOneSql());
        await SeedRolesAsync(context, logger);
        await SeedAdminUserAsync(context, logger);
        await EnsureAdminUserRoleAsync(context, logger);
        await EnsureAdminPasswordAsync(context, logger);
        await SeedAdminUserBranchAsync(context, logger);
        await ExecuteBatchAsync(context, logger, EnsureSeedCreatedByUserSql());
    }

    private static async Task ExecuteBatchAsync(POSDbContext context, ILogger logger, string batch)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(batch);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Core seed batch skipped or partially applied.");
        }
    }

    private static string EnsureCurrenciesTableSql() => """
        IF OBJECT_ID(N'[dbo].[Currencies]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[Currencies] (
                [Id]                  INT            NOT NULL,
                [Code]                NVARCHAR(10)   NOT NULL,
                [Name]                NVARCHAR(100)  NOT NULL,
                [Symbol]              NVARCHAR(10)   NOT NULL,
                [ExchangeRateToPKR]   DECIMAL(18,6)  NOT NULL DEFAULT 1,
                [IsBase]              BIT            NOT NULL DEFAULT 0,
                [IsActive]            BIT            NOT NULL DEFAULT 1,
                CONSTRAINT [PK_Currencies] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [idx_currency_code] ON [dbo].[Currencies]([Code]);
        END
        """;

    private static string SeedCurrenciesSql() => """
        IF NOT EXISTS (SELECT 1 FROM [Currencies] WHERE [Id] = 1)
            INSERT INTO [Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
            VALUES (1, N'PKR', N'Pakistani Rupee', N'₨', 1, 1, 1);
        IF NOT EXISTS (SELECT 1 FROM [Currencies] WHERE [Id] = 2)
            INSERT INTO [Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
            VALUES (2, N'USD', N'US Dollar', N'$', 278, 0, 1);
        IF NOT EXISTS (SELECT 1 FROM [Currencies] WHERE [Id] = 3)
            INSERT INTO [Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
            VALUES (3, N'GBP', N'British Pound', N'£', 350, 0, 1);
        IF NOT EXISTS (SELECT 1 FROM [Currencies] WHERE [Id] = 4)
            INSERT INTO [Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
            VALUES (4, N'AED', N'UAE Dirham', N'د.إ', 75.7, 0, 1);
        IF NOT EXISTS (SELECT 1 FROM [Currencies] WHERE [Id] = 5)
            INSERT INTO [Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
            VALUES (5, N'EUR', N'Euro', N'€', 300, 0, 1);
        """;

    private static string SeedCountriesSql() => """
        IF NOT EXISTS (SELECT 1 FROM [Countries] WHERE [Id] = 1)
            INSERT INTO [Countries] ([Id], [Name], [Code], [IsActive]) VALUES (1, N'United States', N'US', 1);
        IF NOT EXISTS (SELECT 1 FROM [Countries] WHERE [Id] = 2)
            INSERT INTO [Countries] ([Id], [Name], [Code], [IsActive]) VALUES (2, N'United Kingdom', N'GB', 1);
        IF NOT EXISTS (SELECT 1 FROM [Countries] WHERE [Id] = 3)
            INSERT INTO [Countries] ([Id], [Name], [Code], [IsActive]) VALUES (3, N'Pakistan', N'PK', 1);
        IF NOT EXISTS (SELECT 1 FROM [Countries] WHERE [Id] = 4)
            INSERT INTO [Countries] ([Id], [Name], [Code], [IsActive]) VALUES (4, N'United Arab Emirates', N'AE', 1);
        """;

    private static string SeedCitiesSql() => """
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 1)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (1, N'New York', 1, 1);
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 2)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (2, N'Los Angeles', 1, 1);
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 3)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (3, N'London', 2, 1);
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 4)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (4, N'Manchester', 2, 1);
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 5)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (5, N'Karachi', 3, 1);
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 6)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (6, N'Lahore', 3, 1);
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 7)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (7, N'Islamabad', 3, 1);
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 8)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (8, N'Dubai', 4, 1);
        IF NOT EXISTS (SELECT 1 FROM [Cities] WHERE [Id] = 9)
            INSERT INTO [Cities] ([Id], [Name], [CountryId], [IsActive]) VALUES (9, N'Abu Dhabi', 4, 1);
        """;

    private static string SeedBusinessSql() => $"""
        IF NOT EXISTS (SELECT 1 FROM [Businesses] WHERE [Id] = {DefaultBusinessId})
        BEGIN
            SET IDENTITY_INSERT [Businesses] ON;
            INSERT INTO [Businesses] (
                [Id], [Name], [LegalName], [Phone], [Email], [Address], [TaxNumber],
                [Currency], [TimeZone], [IsActive], [IsDeleted], [CreatedDate], [CreatedById])
            VALUES (
                {DefaultBusinessId}, N'AKHSOFT', N'AKHSOFT', N'+923432998052',
                N'owner@restaurant.com', N'123 Main Street', N'NTN-0001',
                N'PKR', N'Asia/Karachi', 1, 0, GETUTCDATE(), {DefaultAdminUserId});
            SET IDENTITY_INSERT [Businesses] OFF;
        END
        IF COL_LENGTH(N'[dbo].[Businesses]', N'CurrencyId') IS NOT NULL
            UPDATE [Businesses]
            SET [CurrencyId] = 1, [Currency] = N'PKR', [TimeZone] = N'Asia/Karachi'
            WHERE [Id] = {DefaultBusinessId}
              AND ([CurrencyId] IS NULL OR [CurrencyId] <= 0 OR [Currency] <> N'PKR');
        """;

    private static string SeedBranchSql() => $"""
        IF NOT EXISTS (SELECT 1 FROM [Branches] WHERE [Id] = {DefaultBranchId})
        BEGIN
            SET IDENTITY_INSERT [Branches] ON;
            INSERT INTO [Branches] (
                [Id], [Name], [Code], [Address], [CountryId], [CityId], [Phone], [Email],
                [OpeningTime], [ClosingTime], [IsActive], [BusinessId], [IsDeleted], [CreatedDate], [CreatedById])
            VALUES (
                {DefaultBranchId}, N'Main Branch', N'MAIN', N'123 Main Street',
                {PakistanCountryId}, {KarachiCityId}, N'+923001234567', N'main@restaurant.com',
                CAST(N'11:00:00' AS time), CAST(N'22:00:00' AS time), 1, {DefaultBusinessId}, 0, GETUTCDATE(), {DefaultAdminUserId});
            SET IDENTITY_INSERT [Branches] OFF;
        END
        """;

    private static string FixLegacyAdminRoleAtIdOneSql() => """
        IF EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 1 AND [Name] = N'Admin')
           AND NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'System Admin')
        BEGIN
            UPDATE [Roles]
            SET [Name] = N'System Admin',
                [Description] = N'Full system access',
                [Permissions] = N'all',
                [IsActive] = 1
            WHERE [Id] = 1;
        END
        """;

    private static async Task SeedRolesAsync(POSDbContext context, ILogger logger)
    {
        var useLegacyTenantColumns = await RolesHaveLegacyTenantColumnsAsync(context);

        foreach (var role in SeedRoles)
        {
            try
            {
                var existingAtId = await context.Roles
                    .IgnoreQueryFilters()
                    .Where(r => r.Id == role.Id)
                    .Select(r => new { r.Name })
                    .FirstOrDefaultAsync();

                if (existingAtId is not null)
                {
                    if (!string.Equals(existingAtId.Name, role.Name, StringComparison.OrdinalIgnoreCase)
                        && !await context.Roles.IgnoreQueryFilters().AnyAsync(r => r.Name == role.Name && !r.IsDeleted))
                    {
                        if (useLegacyTenantColumns)
                        {
                            await context.Database.ExecuteSqlInterpolatedAsync($"""
                                UPDATE [Roles]
                                SET [Name] = {role.Name},
                                    [Description] = {role.Description},
                                    [Permissions] = {role.Permissions},
                                    [IsActive] = 1
                                WHERE [Id] = {role.Id};
                                """);
                        }
                        else
                        {
                            await context.Database.ExecuteSqlInterpolatedAsync($"""
                                UPDATE [Roles]
                                SET [Name] = {role.Name},
                                    [Description] = {role.Description},
                                    [Permissions] = {role.Permissions},
                                    [IsActive] = 1
                                WHERE [Id] = {role.Id};
                                """);
                        }

                        logger.LogInformation("Updated role id {RoleId} to '{RoleName}'.", role.Id, role.Name);
                    }

                    continue;
                }

                var existsByName = await context.Roles
                    .IgnoreQueryFilters()
                    .AnyAsync(r => r.Name == role.Name && !r.IsDeleted);

                if (existsByName)
                    continue;

                var idTaken = await context.Roles
                    .IgnoreQueryFilters()
                    .AnyAsync(r => r.Id == role.Id);

                if (!idTaken)
                {
                    if (useLegacyTenantColumns)
                    {
                        await context.Database.ExecuteSqlInterpolatedAsync($"""
                            SET IDENTITY_INSERT [Roles] ON;
                            INSERT INTO [Roles] ([Id], [Name], [Description], [Permissions], [IsActive], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                            VALUES ({role.Id}, {role.Name}, {role.Description}, {role.Permissions}, 1, 1, 1, {SeedDate}, 0);
                            SET IDENTITY_INSERT [Roles] OFF;
                            """);
                    }
                    else
                    {
                        await context.Database.ExecuteSqlInterpolatedAsync($"""
                            SET IDENTITY_INSERT [Roles] ON;
                            INSERT INTO [Roles] ([Id], [Name], [Description], [Permissions], [IsActive], [CreatedDate], [IsDeleted])
                            VALUES ({role.Id}, {role.Name}, {role.Description}, {role.Permissions}, 1, {SeedDate}, 0);
                            SET IDENTITY_INSERT [Roles] OFF;
                            """);
                    }
                }
                else if (useLegacyTenantColumns)
                {
                    await context.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO [Roles] ([Name], [Description], [Permissions], [IsActive], [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
                        VALUES ({role.Name}, {role.Description}, {role.Permissions}, 1, 1, 1, {SeedDate}, 0);
                        """);
                }
                else
                {
                    await context.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO [Roles] ([Name], [Description], [Permissions], [IsActive], [CreatedDate], [IsDeleted])
                        VALUES ({role.Name}, {role.Description}, {role.Permissions}, 1, {SeedDate}, 0);
                        """);
                }

                logger.LogInformation("Seeded role '{RoleName}'.", role.Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to seed role {RoleName}", role.Name);
            }
        }
    }

    private static async Task<bool> RolesHaveLegacyTenantColumnsAsync(POSDbContext context)
    {
        try
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose)
                await context.Database.OpenConnectionAsync();

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT CASE
                        WHEN COL_LENGTH(N'dbo.Roles', N'BusinessId') IS NOT NULL
                         AND COL_LENGTH(N'dbo.Roles', N'BranchId') IS NOT NULL THEN 1
                        ELSE 0
                    END
                    """;
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) == 1;
            }
            finally
            {
                if (shouldClose)
                    await context.Database.CloseConnectionAsync();
            }
        }
        catch
        {
            return false;
        }
    }

    private static async Task SeedAdminUserAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var adminExists = await context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Username.ToLower() == AdminUsername && !u.IsDeleted);

            if (adminExists)
                return;

            var systemAdminRoleId = await context.Roles
                .IgnoreQueryFilters()
                .Where(r => r.Name == RoleNames.SystemAdmin && r.IsActive && !r.IsDeleted)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (systemAdminRoleId == 0)
            {
                logger.LogWarning("Default admin user was not seeded because the System Admin role is missing.");
                return;
            }

            if (!await context.Businesses.IgnoreQueryFilters().AnyAsync(b => b.Id == DefaultBusinessId && !b.IsDeleted))
            {
                logger.LogWarning("Default admin user was not seeded because business {BusinessId} is missing.", DefaultBusinessId);
                return;
            }

            if (!await context.Branches.IgnoreQueryFilters().AnyAsync(b => b.Id == DefaultBranchId && !b.IsDeleted))
            {
                logger.LogWarning("Default admin user was not seeded because branch {BranchId} is missing.", DefaultBranchId);
                return;
            }

            var idTaken = await context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Id == DefaultAdminUserId);

            if (!idTaken)
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    SET IDENTITY_INSERT [Users] ON;
                    INSERT INTO [Users] (
                        [Id], [FullName], [Username], [PasswordHash], [Phone], [Email], [RoleId], [BusinessId], [BranchId],
                        [IsActive], [Salary], [ShiftType], [Status], [CreatedDate], [CreatedById], [IsDeleted])
                    VALUES (
                        {DefaultAdminUserId}, N'System Administrator', {AdminUsername}, {SeedDefaults.AdminPasswordHash}, N'+923001234567',
                        N'info@infoakhsoft.com', {systemAdminRoleId}, {DefaultBusinessId}, {DefaultBranchId},
                        1, 0, {(int)ShiftType.Flexible}, {(int)UserStatus.Active}, {SeedDate}, {DefaultAdminUserId}, 0);
                    SET IDENTITY_INSERT [Users] OFF;
                    """);
            }
            else
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO [Users] (
                        [FullName], [Username], [PasswordHash], [Phone], [Email], [RoleId], [BusinessId], [BranchId],
                        [IsActive], [Salary], [ShiftType], [Status], [CreatedDate], [CreatedById], [IsDeleted])
                    VALUES (
                        N'System Administrator', {AdminUsername}, {SeedDefaults.AdminPasswordHash}, N'+923001234567',
                        N'info@infoakhsoft.com', {systemAdminRoleId}, {DefaultBusinessId}, {DefaultBranchId},
                        1, 0, {(int)ShiftType.Flexible}, {(int)UserStatus.Active}, {SeedDate}, {DefaultAdminUserId}, 0);
                    """);
            }

            logger.LogInformation("Seeded default admin user '{AdminUsername}'.", AdminUsername);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed default admin user.");
        }
    }

    private static async Task EnsureAdminUserRoleAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var systemAdminRoleId = await context.Roles
                .IgnoreQueryFilters()
                .Where(r => r.Name == RoleNames.SystemAdmin && r.IsActive && !r.IsDeleted)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (systemAdminRoleId == 0)
            {
                logger.LogWarning("Could not assign System Admin role because the role is missing.");
                return;
            }

            foreach (var username in SeedDefaults.SeedAdminUsernames)
            {
                var updated = await context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [Users]
                    SET [RoleId] = {systemAdminRoleId}
                    WHERE LOWER([Username]) = LOWER({username})
                      AND [IsDeleted] = 0
                      AND [RoleId] <> {systemAdminRoleId};
                    """);

                if (updated > 0)
                    logger.LogInformation("Assigned System Admin role to user '{Username}'.", username);
            }

            var updatedPrimary = await context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [Users]
                SET [RoleId] = {systemAdminRoleId}
                WHERE [Id] = {DefaultAdminUserId}
                  AND [IsDeleted] = 0
                  AND [RoleId] <> {systemAdminRoleId};
                """);

            if (updatedPrimary > 0)
                logger.LogInformation("Assigned System Admin role to primary seed user id {UserId}.", DefaultAdminUserId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure System Admin role for seed users.");
        }
    }

    private static async Task EnsureAdminPasswordAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            foreach (var username in SeedDefaults.SeedAdminUsernames)
            {
                var admin = await context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.Username.ToLower() == username.ToLower() && !u.IsDeleted)
                    .Select(u => new { u.Id, u.PasswordHash })
                    .FirstOrDefaultAsync();

                if (admin is null)
                    continue;

                if (BCrypt.Net.BCrypt.Verify(SeedDefaults.AdminPassword, admin.PasswordHash))
                    continue;

                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [Users] SET [PasswordHash] = {SeedDefaults.AdminPasswordHash}
                    WHERE [Id] = {admin.Id};
                    """);

                logger.LogInformation("Updated password for seed user '{Username}'.", username);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure default admin password.");
        }
    }

    private static async Task SeedAdminUserBranchAsync(POSDbContext context, ILogger logger)
    {
        try
        {
            var adminUserId = await context.Users
                .IgnoreQueryFilters()
                .Where(u => SeedDefaults.SeedAdminUsernames.Contains(u.Username) && !u.IsDeleted)
                .OrderBy(u => u.Id)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (adminUserId == 0)
                return;

            await context.Database.ExecuteSqlInterpolatedAsync($"""
                IF OBJECT_ID(N'[dbo].[UserBranches]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM [UserBranches] WHERE [UserId] = {adminUserId} AND [BranchId] = {DefaultBranchId})
                    INSERT INTO [UserBranches] ([UserId], [BranchId]) VALUES ({adminUserId}, {DefaultBranchId});
                """);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed admin user branch assignment.");
        }
    }

    private static string EnsureSeedCreatedByUserSql() => $"""
        DECLARE @SeedUserId INT = {DefaultAdminUserId};
        IF EXISTS (SELECT 1 FROM [Users] WHERE [Id] = @SeedUserId AND [IsDeleted] = 0)
        BEGIN
            IF COL_LENGTH(N'dbo.Businesses', N'CreatedById') IS NOT NULL
                UPDATE [Businesses]
                SET [CreatedById] = @SeedUserId
                WHERE [Id] = {DefaultBusinessId} AND ([CreatedById] IS NULL OR [CreatedById] <= 0);

            IF COL_LENGTH(N'dbo.Branches', N'CreatedById') IS NOT NULL
                UPDATE [Branches]
                SET [CreatedById] = @SeedUserId
                WHERE [Id] = {DefaultBranchId} AND ([CreatedById] IS NULL OR [CreatedById] <= 0);

            IF COL_LENGTH(N'dbo.Users', N'CreatedById') IS NOT NULL
                UPDATE [Users]
                SET [CreatedById] = @SeedUserId
                WHERE [Id] = @SeedUserId AND ([CreatedById] IS NULL OR [CreatedById] <= 0);
        END
        """;
}
