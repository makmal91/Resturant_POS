using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class CustomerInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            // ── Add new columns to the existing Customers table (idempotent) ──
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'CustomerCode')
                ALTER TABLE [dbo].[Customers] ADD [CustomerCode] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Customers_CustomerCode] DEFAULT '';
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'City')
                ALTER TABLE [dbo].[Customers] ADD [City] NVARCHAR(100) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'CNIC')
                ALTER TABLE [dbo].[Customers] ADD [CNIC] NVARCHAR(20) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'CustomerType')
                ALTER TABLE [dbo].[Customers] ADD [CustomerType] INT NOT NULL CONSTRAINT [DF_Customers_CustomerType] DEFAULT 1;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'Status')
                ALTER TABLE [dbo].[Customers] ADD [Status] BIT NOT NULL CONSTRAINT [DF_Customers_Status] DEFAULT 1;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'OpeningBalance')
                ALTER TABLE [dbo].[Customers] ADD [OpeningBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Customers_OpeningBalance] DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'CreditLimit')
                ALTER TABLE [dbo].[Customers] ADD [CreditLimit] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Customers_CreditLimit] DEFAULT 0;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND name = N'IsWalkIn')
                ALTER TABLE [dbo].[Customers] ADD [IsWalkIn] BIT NOT NULL CONSTRAINT [DF_Customers_IsWalkIn] DEFAULT 0;
            """,

            // ── Allow Phone to be NULL (walk-in customer has no phone) ──
            """
            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Customers' AND COLUMN_NAME = 'Phone' AND IS_NULLABLE = 'NO'
            )
            BEGIN
                -- Drop any unique index on Phone before altering
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_branch_phone' AND object_id = OBJECT_ID(N'[dbo].[Customers]'))
                    DROP INDEX [idx_customer_branch_phone] ON [dbo].[Customers];
                ALTER TABLE [dbo].[Customers] ALTER COLUMN [Phone] NVARCHAR(20) NULL;
            END
            """,

            // ── Unique index on (BusinessId, BranchId, Phone) excluding NULLs ──
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_branch_phone_unique' AND object_id = OBJECT_ID(N'[dbo].[Customers]'))
                CREATE UNIQUE INDEX [idx_customer_branch_phone_unique]
                    ON [dbo].[Customers] ([BusinessId], [BranchId], [Phone])
                    WHERE [Phone] IS NOT NULL AND [IsDeleted] = 0;
            """,

            // ── Walk-in customer index ──
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_walkin' AND object_id = OBJECT_ID(N'[dbo].[Customers]'))
                CREATE INDEX [idx_customer_walkin] ON [dbo].[Customers] ([BusinessId], [BranchId], [IsWalkIn]);
            """,

            // ── Patch Customers menu to use Customers module ──
            """
            UPDATE [dbo].[Menus]
            SET [ModuleName] = N'Customers'
            WHERE [Route] = N'/customers'
              AND ([ModuleName] IS NULL OR [ModuleName] = N'');
            """
        };

        foreach (var sql in batches)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "CustomerInitializer batch skipped or partially applied.");
            }
        }
    }

    public static async Task SeedWalkInCustomersAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            INSERT INTO [dbo].[Customers]
                ([CustomerCode], [Name], [Phone], [Email], [Address], [CustomerType], [Status], [IsWalkIn],
                 [OpeningBalance], [CreditLimit], [LoyaltyPoints],
                 [BusinessId], [BranchId], [CreatedDate], [IsDeleted])
            SELECT
                N'CUS-00000', N'Walk-In Customer', NULL, N'', N'', 1, 1, 1,
                0, 0, 0,
                b.[BusinessId], b.[Id], GETUTCDATE(), 0
            FROM [dbo].[Branches] b
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[Customers] c
                  WHERE c.[IsWalkIn] = 1
                    AND c.[BusinessId] = b.[BusinessId]
                    AND c.[BranchId]  = b.[Id]
                    AND c.[IsDeleted] = 0
              );
            """,
            """
            UPDATE [dbo].[Customers]
            SET [CustomerCode] = N'CUS-00000', [Name] = N'Walk-In Customer'
            WHERE [IsWalkIn] = 1 AND [IsDeleted] = 0
              AND ([CustomerCode] = N'WALK-IN' OR [CustomerCode] = N'' OR [Name] = N'Walk-in Customer');
            """
        };

        foreach (var sql in batches)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Walk-in customer seed batch skipped or partially applied.");
            }
        }
    }
}
