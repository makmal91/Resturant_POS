using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

/// <summary>
/// Ensures foundational tables exist when EF migrations did not run (empty/partial database).
/// </summary>
public static class CoreDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[Countries]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Countries] (
                    [Id]       INT           NOT NULL PRIMARY KEY,
                    [Name]     NVARCHAR(100) NOT NULL,
                    [Code]     NVARCHAR(10)  NOT NULL,
                    [IsActive] BIT           NOT NULL CONSTRAINT [DF_Countries_IsActive] DEFAULT 1
                );
                CREATE UNIQUE INDEX [idx_country_code] ON [dbo].[Countries]([Code]);
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[Cities]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Cities] (
                    [Id]        INT           NOT NULL PRIMARY KEY,
                    [Name]      NVARCHAR(100) NOT NULL,
                    [CountryId] INT           NOT NULL,
                    [IsActive]  BIT           NOT NULL CONSTRAINT [DF_Cities_IsActive] DEFAULT 1
                );
                CREATE INDEX [idx_city_countryid] ON [dbo].[Cities]([CountryId]);
                CREATE UNIQUE INDEX [idx_city_country_name] ON [dbo].[Cities]([CountryId], [Name]);
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[Businesses]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Businesses] (
                    [Id]              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name]            NVARCHAR(200) NOT NULL,
                    [LegalName]       NVARCHAR(250) NOT NULL CONSTRAINT [DF_Businesses_LegalName] DEFAULT N'',
                    [Logo]            VARBINARY(MAX) NULL,
                    [LogoFileName]    NVARCHAR(255) NULL,
                    [LogoContentType] NVARCHAR(100) NULL,
                    [Phone]           NVARCHAR(20) NOT NULL CONSTRAINT [DF_Businesses_Phone] DEFAULT N'',
                    [Email]           NVARCHAR(100) NOT NULL CONSTRAINT [DF_Businesses_Email] DEFAULT N'',
                    [Address]         NVARCHAR(500) NOT NULL CONSTRAINT [DF_Businesses_Address] DEFAULT N'',
                    [TaxNumber]       NVARCHAR(50) NOT NULL CONSTRAINT [DF_Businesses_TaxNumber] DEFAULT N'',
                    [Currency]        NVARCHAR(10) NOT NULL CONSTRAINT [DF_Businesses_Currency] DEFAULT N'PKR',
                    [TimeZone]        NVARCHAR(100) NOT NULL CONSTRAINT [DF_Businesses_TimeZone] DEFAULT N'UTC',
                    [IsActive]        BIT NOT NULL CONSTRAINT [DF_Businesses_IsActive] DEFAULT 1,
                    [IsDeleted]       BIT NOT NULL CONSTRAINT [DF_Businesses_IsDeleted] DEFAULT 0,
                    [CreatedDate]     DATETIME2 NOT NULL CONSTRAINT [DF_Businesses_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById]     INT NULL,
                    [UpdatedDate]     DATETIME2 NULL,
                    [ModifiedById]    INT NULL
                );
                CREATE INDEX [idx_business_name] ON [dbo].[Businesses]([Name]);
                CREATE INDEX [idx_business_email] ON [dbo].[Businesses]([Email]);
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[Branches]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Branches] (
                    [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name]         NVARCHAR(100) NOT NULL,
                    [Code]         NVARCHAR(20)  NOT NULL,
                    [Address]      NVARCHAR(500) NOT NULL CONSTRAINT [DF_Branches_Address] DEFAULT N'',
                    [CountryId]    INT NOT NULL CONSTRAINT [DF_Branches_CountryId] DEFAULT 3,
                    [CityId]       INT NOT NULL CONSTRAINT [DF_Branches_CityId] DEFAULT 5,
                    [Phone]        NVARCHAR(20)  NOT NULL CONSTRAINT [DF_Branches_Phone] DEFAULT N'',
                    [Email]        NVARCHAR(100) NOT NULL CONSTRAINT [DF_Branches_Email] DEFAULT N'',
                    [OpeningTime]  TIME NOT NULL CONSTRAINT [DF_Branches_OpeningTime] DEFAULT '11:00:00',
                    [ClosingTime]  TIME NOT NULL CONSTRAINT [DF_Branches_ClosingTime] DEFAULT '22:00:00',
                    [IsActive]     BIT NOT NULL CONSTRAINT [DF_Branches_IsActive] DEFAULT 1,
                    [BusinessId]   INT NOT NULL CONSTRAINT [DF_Branches_BusinessId] DEFAULT 1,
                    [IsDeleted]    BIT NOT NULL CONSTRAINT [DF_Branches_IsDeleted] DEFAULT 0,
                    [CreatedDate]  DATETIME2 NOT NULL CONSTRAINT [DF_Branches_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById]  INT NULL,
                    [UpdatedDate]  DATETIME2 NULL,
                    [ModifiedById] INT NULL
                );
                CREATE UNIQUE INDEX [idx_branch_code] ON [dbo].[Branches]([Code]);
                CREATE INDEX [idx_branch_businessid] ON [dbo].[Branches]([BusinessId]);
                CREATE INDEX [idx_branch_countryid] ON [dbo].[Branches]([CountryId]);
                CREATE INDEX [idx_branch_cityid] ON [dbo].[Branches]([CityId]);
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[Roles]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Roles] (
                    [Id]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name]        NVARCHAR(100) NOT NULL,
                    [Description] NVARCHAR(500) NOT NULL CONSTRAINT [DF_Roles_Description] DEFAULT N'',
                    [Permissions] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_Roles_Permissions] DEFAULT N'',
                    [IsActive]    BIT NOT NULL CONSTRAINT [DF_Roles_IsActive] DEFAULT 1,
                    [IsDeleted]   BIT NOT NULL CONSTRAINT [DF_Roles_IsDeleted] DEFAULT 0,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Roles_CreatedDate] DEFAULT GETUTCDATE(),
                    [UpdatedDate] DATETIME2 NULL
                );
                CREATE UNIQUE INDEX [idx_role_name] ON [dbo].[Roles]([Name]);
            END
            """,
            """
            IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Users] (
                    [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [FullName]     NVARCHAR(150) NOT NULL,
                    [Username]     NVARCHAR(50)  NOT NULL,
                    [PasswordHash] NVARCHAR(MAX) NOT NULL,
                    [Phone]        NVARCHAR(20)  NOT NULL CONSTRAINT [DF_Users_Phone] DEFAULT N'',
                    [Email]        NVARCHAR(100) NOT NULL,
                    [RoleId]       INT NOT NULL,
                    [BusinessId]   INT NOT NULL CONSTRAINT [DF_Users_BusinessId] DEFAULT 1,
                    [BranchId]     INT NOT NULL CONSTRAINT [DF_Users_BranchId] DEFAULT 1,
                    [IsActive]     BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT 1,
                    [DeletedAt]    DATETIME2 NULL,
                    [Salary]       DECIMAL(10,2) NOT NULL CONSTRAINT [DF_Users_Salary] DEFAULT 0,
                    [ShiftType]    INT NOT NULL CONSTRAINT [DF_Users_ShiftType] DEFAULT 4,
                    [Status]       INT NOT NULL CONSTRAINT [DF_Users_Status] DEFAULT 0,
                    [IsDeleted]    BIT NOT NULL CONSTRAINT [DF_Users_IsDeleted] DEFAULT 0,
                    [CreatedDate]  DATETIME2 NOT NULL CONSTRAINT [DF_Users_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById]  INT NULL,
                    [UpdatedDate]  DATETIME2 NULL,
                    [ModifiedById] INT NULL
                );
                CREATE UNIQUE INDEX [idx_user_username] ON [dbo].[Users]([Username]);
                CREATE UNIQUE INDEX [idx_user_email] ON [dbo].[Users]([Email]);
                CREATE INDEX [idx_user_branchid] ON [dbo].[Users]([BranchId]);
                CREATE INDEX [idx_user_businessid] ON [dbo].[Users]([BusinessId]);
                CREATE INDEX [idx_user_roleid] ON [dbo].[Users]([RoleId]);
            END
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
                logger.LogWarning(ex, "Core schema batch skipped or partially applied.");
            }
        }
    }
}
