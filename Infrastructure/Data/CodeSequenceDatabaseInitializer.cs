using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class CodeSequenceDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[CodeSequences]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CodeSequences] (
                    [Id] int NOT NULL IDENTITY,
                    [ModuleName] nvarchar(50) NOT NULL,
                    [BranchId] int NULL,
                    [Prefix] nvarchar(20) NOT NULL,
                    [LastNumber] bigint NOT NULL DEFAULT 0,
                    [ResetType] int NOT NULL DEFAULT 0,
                    [LastResetDate] datetime2 NULL,
                    CONSTRAINT [PK_CodeSequences] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [idx_codesequence_module_branch]
                    ON [dbo].[CodeSequences] ([ModuleName], [BranchId]);
            END;
            """,

            // Sync Branch sequence (global)
            """
            IF NOT EXISTS (SELECT 1 FROM [dbo].[CodeSequences] WHERE [ModuleName] = N'Branch' AND [BranchId] IS NULL)
            BEGIN
                DECLARE @branchMax bigint = ISNULL((
                    SELECT MAX(TRY_CAST(SUBSTRING([Code], 4, 20) AS bigint))
                    FROM [dbo].[Branches]
                    WHERE [IsDeleted] = 0 AND [Code] LIKE N'BR-%'
                ), 0);
                IF @branchMax = 0
                    SET @branchMax = ISNULL((SELECT COUNT(*) FROM [dbo].[Branches] WHERE [IsDeleted] = 0), 0);
                INSERT INTO [dbo].[CodeSequences] ([ModuleName], [BranchId], [Prefix], [LastNumber], [ResetType], [LastResetDate])
                VALUES (N'Branch', NULL, N'BR', @branchMax, 0, GETUTCDATE());
            END;
            """,

            // Sync Category sequences per branch
            """
            INSERT INTO [dbo].[CodeSequences] ([ModuleName], [BranchId], [Prefix], [LastNumber], [ResetType], [LastResetDate])
            SELECT N'Category', b.[Id], N'CAT',
                ISNULL((
                    SELECT MAX(TRY_CAST(SUBSTRING(c.[Code], 5, 20) AS bigint))
                    FROM [dbo].[MenuCategories] c
                    WHERE c.[BranchId] = b.[Id] AND c.[IsDeleted] = 0 AND c.[Code] LIKE N'CAT-%'
                ), ISNULL((
                    SELECT COUNT(*) FROM [dbo].[MenuCategories] c2
                    WHERE c2.[BranchId] = b.[Id] AND c2.[IsDeleted] = 0 AND c2.[Code] <> N''
                ), 0)),
                0, GETUTCDATE()
            FROM [dbo].[Branches] b
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[CodeSequences] s
                  WHERE s.[ModuleName] = N'Category' AND s.[BranchId] = b.[Id]
              );
            """,

            // Sync SubCategory sequences per branch
            """
            INSERT INTO [dbo].[CodeSequences] ([ModuleName], [BranchId], [Prefix], [LastNumber], [ResetType], [LastResetDate])
            SELECT N'SubCategory', b.[Id], N'SUB',
                ISNULL((
                    SELECT MAX(TRY_CAST(SUBSTRING(sc.[Code], 5, 20) AS bigint))
                    FROM [dbo].[SubCategories] sc
                    WHERE sc.[BranchId] = b.[Id] AND sc.[IsDeleted] = 0 AND sc.[Code] LIKE N'SUB-%'
                ), ISNULL((
                    SELECT COUNT(*) FROM [dbo].[SubCategories] sc2
                    WHERE sc2.[BranchId] = b.[Id] AND sc2.[IsDeleted] = 0 AND sc2.[Code] <> N''
                ), 0)),
                0, GETUTCDATE()
            FROM [dbo].[Branches] b
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[CodeSequences] s
                  WHERE s.[ModuleName] = N'SubCategory' AND s.[BranchId] = b.[Id]
              );
            """,

            // Sync Product sequences per branch
            """
            INSERT INTO [dbo].[CodeSequences] ([ModuleName], [BranchId], [Prefix], [LastNumber], [ResetType], [LastResetDate])
            SELECT N'Product', b.[Id], N'PRD',
                ISNULL((
                    SELECT MAX(TRY_CAST(SUBSTRING(p.[ProductCode], 5, 20) AS bigint))
                    FROM [dbo].[Products] p
                    WHERE p.[BranchId] = b.[Id] AND p.[IsDeleted] = 0 AND p.[ProductCode] LIKE N'PRD-%'
                ), ISNULL((
                    SELECT COUNT(*) FROM [dbo].[Products] p2
                    WHERE p2.[BranchId] = b.[Id] AND p2.[IsDeleted] = 0
                ), 0)),
                0, GETUTCDATE()
            FROM [dbo].[Branches] b
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[CodeSequences] s
                  WHERE s.[ModuleName] = N'Product' AND s.[BranchId] = b.[Id]
              );
            """,

            // Sync Customer sequences per branch (exclude walk-in CUS-00000)
            """
            INSERT INTO [dbo].[CodeSequences] ([ModuleName], [BranchId], [Prefix], [LastNumber], [ResetType], [LastResetDate])
            SELECT N'Customer', b.[Id], N'CUS',
                ISNULL((
                    SELECT MAX(TRY_CAST(SUBSTRING(c.[CustomerCode], 5, 20) AS bigint))
                    FROM [dbo].[Customers] c
                    WHERE c.[BranchId] = b.[Id] AND c.[IsDeleted] = 0
                      AND c.[CustomerCode] LIKE N'CUS-%' AND c.[IsWalkIn] = 0
                ), ISNULL((
                    SELECT COUNT(*) FROM [dbo].[Customers] c2
                    WHERE c2.[BranchId] = b.[Id] AND c2.[IsDeleted] = 0 AND c2.[IsWalkIn] = 0
                ), 0)),
                0, GETUTCDATE()
            FROM [dbo].[Branches] b
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[CodeSequences] s
                  WHERE s.[ModuleName] = N'Customer' AND s.[BranchId] = b.[Id]
              );
            """,

            // Sync Purchase sequences per branch (current month)
            """
            INSERT INTO [dbo].[CodeSequences] ([ModuleName], [BranchId], [Prefix], [LastNumber], [ResetType], [LastResetDate])
            SELECT N'Purchase', b.[Id], N'PUR',
                ISNULL((
                    SELECT MAX(TRY_CAST(RIGHT(p.[InvoiceNo], 4) AS bigint))
                    FROM [dbo].[Purchases] p
                    WHERE p.[BranchId] = b.[Id] AND p.[IsDeleted] = 0
                      AND p.[InvoiceNo] LIKE N'PUR-' + FORMAT(GETUTCDATE(), 'yyyyMM') + N'-%'
                ), 0),
                2, GETUTCDATE()
            FROM [dbo].[Branches] b
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[CodeSequences] s
                  WHERE s.[ModuleName] = N'Purchase' AND s.[BranchId] = b.[Id]
              );
            """,

            // Sync SalesInvoice sequences per branch (today)
            """
            INSERT INTO [dbo].[CodeSequences] ([ModuleName], [BranchId], [Prefix], [LastNumber], [ResetType], [LastResetDate])
            SELECT N'SalesInvoice', b.[Id], N'INV',
                ISNULL((
                    SELECT MAX(TRY_CAST(RIGHT(s.[InvoiceNo], 4) AS bigint))
                    FROM [dbo].[SaleInvoices] s
                    WHERE s.[BranchId] = b.[Id] AND s.[IsDeleted] = 0
                      AND s.[InvoiceNo] LIKE N'INV-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + N'-%'
                ), ISNULL((
                    SELECT MAX(TRY_CAST(RIGHT(s2.[InvoiceNo], 5) AS bigint))
                    FROM [dbo].[SaleInvoices] s2
                    WHERE s2.[BranchId] = b.[Id] AND s2.[IsDeleted] = 0
                      AND s2.[InvoiceNo] LIKE N'SI-' + FORMAT(GETUTCDATE(), 'yyyyMMdd') + N'-%'
                ), 0)),
                1, GETUTCDATE()
            FROM [dbo].[Branches] b
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[CodeSequences] s
                  WHERE s.[ModuleName] = N'SalesInvoice' AND s.[BranchId] = b.[Id]
              );
            """,

            // Add SupplierCode column
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = N'SupplierCode')
                ALTER TABLE [dbo].[Suppliers] ADD [SupplierCode] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Suppliers_SupplierCode] DEFAULT N'';
            """,

            // Unique index on SupplierCode per branch
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_supplier_branch_code' AND object_id = OBJECT_ID(N'[dbo].[Suppliers]'))
                CREATE UNIQUE INDEX [idx_supplier_branch_code]
                    ON [dbo].[Suppliers] ([BusinessId], [BranchId], [SupplierCode])
                    WHERE [SupplierCode] <> N'' AND [IsDeleted] = 0;
            """,

            // Unique index on CustomerCode per branch
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_branch_code' AND object_id = OBJECT_ID(N'[dbo].[Customers]'))
                CREATE UNIQUE INDEX [idx_customer_branch_code]
                    ON [dbo].[Customers] ([BusinessId], [BranchId], [CustomerCode])
                    WHERE [CustomerCode] <> N'' AND [IsDeleted] = 0;
            """,

            // Unique index on SubCategory code per branch
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_subcategory_branch_code' AND object_id = OBJECT_ID(N'[dbo].[SubCategories]'))
                CREATE UNIQUE INDEX [idx_subcategory_branch_code]
                    ON [dbo].[SubCategories] ([BranchId], [Code])
                    WHERE [Code] IS NOT NULL AND [Code] <> N'' AND [IsDeleted] = 0;
            """,

            // Update walk-in customer code to CUS-00000
            """
            UPDATE [dbo].[Customers]
            SET [CustomerCode] = N'CUS-00000', [Name] = N'Walk-In Customer'
            WHERE [IsWalkIn] = 1 AND [IsDeleted] = 0
              AND ([CustomerCode] = N'WALK-IN' OR [CustomerCode] = N'' OR [Name] = N'Walk-in Customer');
            """,

            // Sync Supplier sequences per branch
            """
            INSERT INTO [dbo].[CodeSequences] ([ModuleName], [BranchId], [Prefix], [LastNumber], [ResetType], [LastResetDate])
            SELECT N'Supplier', b.[Id], N'SUP',
                ISNULL((
                    SELECT MAX(TRY_CAST(SUBSTRING(s.[SupplierCode], 5, 20) AS bigint))
                    FROM [dbo].[Suppliers] s
                    WHERE s.[BranchId] = b.[Id] AND s.[IsDeleted] = 0 AND s.[SupplierCode] LIKE N'SUP-%'
                ), ISNULL((
                    SELECT COUNT(*) FROM [dbo].[Suppliers] s2
                    WHERE s2.[BranchId] = b.[Id] AND s2.[IsDeleted] = 0
                ), 0)),
                0, GETUTCDATE()
            FROM [dbo].[Branches] b
            WHERE b.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[CodeSequences] cs
                  WHERE cs.[ModuleName] = N'Supplier' AND cs.[BranchId] = b.[Id]
              );
            """,

            // Re-sync existing Category sequences when data is ahead of the counter
            """
            UPDATE s
            SET s.[LastNumber] = src.[MaxNum], s.[LastResetDate] = GETUTCDATE()
            FROM [dbo].[CodeSequences] s
            INNER JOIN (
                SELECT b.[Id] AS [BranchId],
                    ISNULL(MAX(TRY_CAST(SUBSTRING(c.[Code], 5, 20) AS bigint)), 0) AS [MaxNum]
                FROM [dbo].[Branches] b
                LEFT JOIN [dbo].[MenuCategories] c
                    ON c.[BranchId] = b.[Id] AND c.[IsDeleted] = 0 AND c.[Code] LIKE N'CAT-%'
                WHERE b.[IsDeleted] = 0
                GROUP BY b.[Id]
            ) src ON s.[BranchId] = src.[BranchId]
            WHERE s.[ModuleName] = N'Category' AND s.[LastNumber] < src.[MaxNum];
            """,

            // Re-sync existing SubCategory sequences when data is ahead of the counter
            """
            UPDATE s
            SET s.[LastNumber] = src.[MaxNum], s.[LastResetDate] = GETUTCDATE()
            FROM [dbo].[CodeSequences] s
            INNER JOIN (
                SELECT b.[Id] AS [BranchId],
                    ISNULL(MAX(TRY_CAST(SUBSTRING(sc.[Code], 5, 20) AS bigint)), 0) AS [MaxNum]
                FROM [dbo].[Branches] b
                LEFT JOIN [dbo].[SubCategories] sc
                    ON sc.[BranchId] = b.[Id] AND sc.[IsDeleted] = 0 AND sc.[Code] LIKE N'SUB-%'
                WHERE b.[IsDeleted] = 0
                GROUP BY b.[Id]
            ) src ON s.[BranchId] = src.[BranchId]
            WHERE s.[ModuleName] = N'SubCategory' AND s.[LastNumber] < src.[MaxNum];
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
                logger.LogWarning(ex, "CodeSequenceDatabaseInitializer batch skipped or partially applied.");
            }
        }
    }
}
