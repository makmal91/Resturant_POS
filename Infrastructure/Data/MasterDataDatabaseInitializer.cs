using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class MasterDataDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            // ── Currencies master table ──
            """
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
            """,

            // ── Business.CurrencyId FK column (add column + index only; data sync runs in seed) ──
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Businesses]') AND name = N'CurrencyId')
                ALTER TABLE [dbo].[Businesses] ADD [CurrencyId] INT NOT NULL CONSTRAINT [DF_Businesses_CurrencyId] DEFAULT 1;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_business_currencyid' AND object_id = OBJECT_ID(N'[dbo].[Businesses]'))
                CREATE INDEX [idx_business_currencyid] ON [dbo].[Businesses]([CurrencyId]);
            """,

            // ── ExpenseCategories master table ──
            """
            IF OBJECT_ID(N'[dbo].[ExpenseCategories]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ExpenseCategories] (
                    [Id]            INT            IDENTITY(1,1) NOT NULL,
                    [BusinessId]    INT            NOT NULL DEFAULT 1,
                    [BranchId]      INT            NOT NULL DEFAULT 1,
                    [Name]          NVARCHAR(100)  NOT NULL,
                    [Description]   NVARCHAR(500)  NULL,
                    [Status]        BIT            NOT NULL DEFAULT 1,
                    [CreatedDate]   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    [CreatedById]   INT            NULL,
                    [UpdatedDate]   DATETIME2      NULL,
                    [ModifiedById]  INT            NULL,
                    [IsDeleted]     BIT            NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_ExpenseCategories] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ExpenseCategories_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches]([Id])
                );
                CREATE UNIQUE INDEX [idx_expensecategory_branch_name]
                    ON [dbo].[ExpenseCategories]([BusinessId],[BranchId],[Name])
                    WHERE [IsDeleted] = 0;
            END
            """,

            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ExpenseCategories]') AND name = N'GlAccountId')
                ALTER TABLE [dbo].[ExpenseCategories] ADD [GlAccountId] INT NULL;
            """,

            // ── Expenses: add ExpenseCategoryId, migrate from CategoryName ──
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND name = N'ExpenseCategoryId')
                ALTER TABLE [dbo].[Expenses] ADD [ExpenseCategoryId] INT NULL;
            """,

            // Create categories from legacy free-text CategoryName values (dynamic SQL: column may not exist on EF-created DBs)
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND name = N'CategoryName')
            EXEC(N'
                INSERT INTO [dbo].[ExpenseCategories] ([Name],[BusinessId],[BranchId],[Status],[CreatedDate],[IsDeleted])
                SELECT DISTINCT LTRIM(RTRIM(e.[CategoryName])), e.[BusinessId], e.[BranchId], 1, GETUTCDATE(), 0
                FROM [dbo].[Expenses] e
                WHERE e.[IsDeleted] = 0
                  AND LTRIM(RTRIM(ISNULL(e.[CategoryName], N''''))) <> N''''
                  AND NOT EXISTS (
                      SELECT 1 FROM [dbo].[ExpenseCategories] ec
                      WHERE ec.[BranchId] = e.[BranchId] AND ec.[BusinessId] = e.[BusinessId]
                        AND ec.[Name] = LTRIM(RTRIM(e.[CategoryName])) AND ec.[IsDeleted] = 0
                  );
            ');
            """,

            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND name = N'CategoryName')
            EXEC(N'
                UPDATE e SET e.[ExpenseCategoryId] = ec.[Id]
                FROM [dbo].[Expenses] e
                INNER JOIN [dbo].[ExpenseCategories] ec
                    ON ec.[BranchId] = e.[BranchId]
                   AND ec.[BusinessId] = e.[BusinessId]
                   AND ec.[Name] = LTRIM(RTRIM(e.[CategoryName]))
                   AND ec.[IsDeleted] = 0
                WHERE e.[ExpenseCategoryId] IS NULL AND e.[IsDeleted] = 0;
            ');
            """,

            // Fallback unmapped expenses to "Other" (skip when categories not seeded yet)
            """
            IF OBJECT_ID(N'[dbo].[ExpenseCategories]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[Expenses]', N'U') IS NOT NULL
               AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND name = N'ExpenseCategoryId')
            BEGIN
                UPDATE e SET e.[ExpenseCategoryId] = ec.[Id]
                FROM [dbo].[Expenses] e
                INNER JOIN [dbo].[ExpenseCategories] ec
                    ON ec.[BranchId] = e.[BranchId]
                   AND ec.[BusinessId] = e.[BusinessId]
                   AND ec.[Name] = N'Other'
                   AND ec.[IsDeleted] = 0
                WHERE e.[ExpenseCategoryId] IS NULL AND e.[IsDeleted] = 0;
            END
            """,

            """
            IF OBJECT_ID(N'[dbo].[Expenses]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND name = N'ExpenseCategoryId')
                   AND NOT EXISTS (SELECT 1 FROM [dbo].[Expenses] WHERE [ExpenseCategoryId] IS NULL AND [IsDeleted] = 0)
                BEGIN
                    ALTER TABLE [dbo].[Expenses] ALTER COLUMN [ExpenseCategoryId] INT NOT NULL;
                END
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_expenses_category' AND object_id = OBJECT_ID(N'[dbo].[Expenses]'))
                    DROP INDEX [idx_expenses_category] ON [dbo].[Expenses];
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_expenses_expensecategory' AND object_id = OBJECT_ID(N'[dbo].[Expenses]'))
                    CREATE INDEX [idx_expenses_expensecategory] ON [dbo].[Expenses]([BusinessId],[BranchId],[ExpenseCategoryId]);
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Expenses]') AND name = N'CategoryName')
                    EXEC(N'ALTER TABLE [dbo].[Expenses] DROP COLUMN [CategoryName];');
            END
            """,

            // ── Customer: CountryId + CityId FK columns ──
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'CountryId')
                ALTER TABLE [dbo].[Customers] ADD [CountryId] INT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'CityId')
                ALTER TABLE [dbo].[Customers] ADD [CityId] INT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_countryid' AND object_id = OBJECT_ID(N'[dbo].[Customers]'))
                CREATE INDEX [idx_customer_countryid] ON [dbo].[Customers]([CountryId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_cityid' AND object_id = OBJECT_ID(N'[dbo].[Customers]'))
                CREATE INDEX [idx_customer_cityid] ON [dbo].[Customers]([CityId]);
            """,

            // Migrate existing free-text City to CityId where possible (dynamic SQL: City column removed in EF schema)
            """
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'City')
            EXEC(N'
                UPDATE c SET c.[CityId] = ci.[Id], c.[CountryId] = ci.[CountryId]
                FROM [dbo].[Customers] c
                INNER JOIN [dbo].[Cities] ci ON LOWER(LTRIM(RTRIM(ci.[Name]))) = LOWER(LTRIM(RTRIM(c.[City])))
                WHERE c.[CityId] IS NULL AND c.[City] IS NOT NULL AND LTRIM(RTRIM(c.[City])) <> N'''';
            ');
            """
        };

        await SqlSchemaBatchRunner.ExecuteAsync(context, logger, "MasterData", batches);
    }

    public static async Task SeedReferenceDataAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Currencies] WHERE [Id] = 1)
                INSERT INTO [dbo].[Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
                VALUES (1, N'PKR', N'Pakistani Rupee', N'₨', 1, 1, 1);
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Currencies] WHERE [Id] = 2)
                INSERT INTO [dbo].[Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
                VALUES (2, N'USD', N'US Dollar', N'$', 278, 0, 1);
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Currencies] WHERE [Id] = 3)
                INSERT INTO [dbo].[Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
                VALUES (3, N'GBP', N'British Pound', N'£', 350, 0, 1);
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Currencies] WHERE [Id] = 4)
                INSERT INTO [dbo].[Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
                VALUES (4, N'AED', N'UAE Dirham', N'د.إ', 75.7, 0, 1);
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Currencies] WHERE [Id] = 5)
                INSERT INTO [dbo].[Currencies] ([Id],[Code],[Name],[Symbol],[ExchangeRateToPKR],[IsBase],[IsActive])
                VALUES (5, N'EUR', N'Euro', N'€', 300, 0, 1);
            """,
            """
            IF COL_LENGTH(N'dbo.Businesses', N'CurrencyId') IS NOT NULL
            EXEC(N'
                UPDATE b SET b.[CurrencyId] = c.[Id]
                FROM [dbo].[Businesses] b
                INNER JOIN [dbo].[Currencies] c ON c.[Code] = UPPER(LTRIM(RTRIM(ISNULL(b.[Currency], N''PKR''))))
                WHERE b.[CurrencyId] = 1 AND UPPER(LTRIM(RTRIM(ISNULL(b.[Currency], N'''')))) <> N''PKR'';
                UPDATE [dbo].[Businesses] SET [Currency] = N''PKR'', [CurrencyId] = 1 WHERE [CurrencyId] IS NULL OR [CurrencyId] <= 0;
            ');
            """,
            """
            INSERT INTO [dbo].[ExpenseCategories] ([Name],[Description],[Status],[BusinessId],[BranchId],[CreatedDate],[IsDeleted])
            SELECT cat.[Name], cat.[Description], 1, b.[BusinessId], b.[Id], GETUTCDATE(), 0
            FROM [dbo].[Branches] b
            CROSS JOIN (VALUES
                (N'Utilities', N'Electricity, water, gas'),
                (N'Rent', N'Property rent'),
                (N'Salary', N'Staff salaries'),
                (N'Supplies', N'Office and operational supplies'),
                (N'Maintenance', N'Repairs and maintenance'),
                (N'Other', N'Miscellaneous expenses')
            ) AS cat([Name],[Description])
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[ExpenseCategories] ec
                  WHERE ec.[BranchId] = b.[Id] AND ec.[BusinessId] = b.[BusinessId]
                    AND ec.[Name] = cat.[Name] AND ec.[IsDeleted] = 0
              );
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
                logger.LogWarning(ex, "MasterData seed batch skipped or partially applied.");
            }
        }
    }
}
