using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class BrandDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF OBJECT_ID(N'[dbo].[Brands]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Brands] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(100) NOT NULL,
                    [Description] NVARCHAR(500) NOT NULL CONSTRAINT [DF_Brands_Description] DEFAULT N'',
                    [ImageData] VARBINARY(MAX) NULL,
                    [ImageContentType] NVARCHAR(100) NULL,
                    [ImageFileName] NVARCHAR(255) NULL,
                    [Status] BIT NOT NULL CONSTRAINT [DF_Brands_Status] DEFAULT 1,
                    [BusinessId] INT NOT NULL CONSTRAINT [DF_Brands_BusinessId] DEFAULT 1,
                    [CreatedDate] DATETIME2 NOT NULL CONSTRAINT [DF_Brands_CreatedDate] DEFAULT GETUTCDATE(),
                    [CreatedById] INT NULL,
                    [CreatedByName] NVARCHAR(MAX) NULL,
                    [UpdatedDate] DATETIME2 NULL,
                    [ModifiedById] INT NULL,
                    [ModifiedByName] NVARCHAR(MAX) NULL,
                    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Brands_IsDeleted] DEFAULT 0,
                    [BranchId] INT NOT NULL,
                    CONSTRAINT [FK_Brands_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id])
                );
            END
            """,
            SyncLegacyBrandSchemaSql(),
            """
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_brand_branchid' AND object_id = OBJECT_ID(N'dbo.Brands'))
                CREATE INDEX [idx_brand_branchid] ON [dbo].[Brands]([BranchId]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_brand_branch_name' AND object_id = OBJECT_ID(N'dbo.Brands'))
                CREATE UNIQUE INDEX [idx_brand_branch_name] ON [dbo].[Brands]([BranchId], [Name]);
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
                logger.LogWarning(ex, "Brand schema batch skipped or partially applied.");
            }
        }
    }

    private static string SyncLegacyBrandSchemaSql() => """
        IF OBJECT_ID(N'[dbo].[Brands]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Brands', N'Status') IS NULL
               AND COL_LENGTH(N'dbo.Brands', N'IsActive') IS NOT NULL
                EXEC sp_rename N'dbo.Brands.IsActive', N'Status', N'COLUMN';

            IF COL_LENGTH(N'dbo.Brands', N'ImageData') IS NULL
                ALTER TABLE [dbo].[Brands] ADD [ImageData] VARBINARY(MAX) NULL;

            IF COL_LENGTH(N'dbo.Brands', N'ImageContentType') IS NULL
                ALTER TABLE [dbo].[Brands] ADD [ImageContentType] NVARCHAR(100) NULL;

            IF COL_LENGTH(N'dbo.Brands', N'ImageFileName') IS NULL
                ALTER TABLE [dbo].[Brands] ADD [ImageFileName] NVARCHAR(255) NULL;

            IF COL_LENGTH(N'dbo.Brands', N'Status') IS NULL
                ALTER TABLE [dbo].[Brands] ADD [Status] BIT NOT NULL
                    CONSTRAINT [DF_Brands_Status_Legacy] DEFAULT 1;

            IF COL_LENGTH(N'dbo.Brands', N'CreatedByName') IS NULL
                ALTER TABLE [dbo].[Brands] ADD [CreatedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.Brands', N'ModifiedByName') IS NULL
                ALTER TABLE [dbo].[Brands] ADD [ModifiedByName] NVARCHAR(MAX) NULL;

            IF COL_LENGTH(N'dbo.Brands', N'Description') IS NULL
                ALTER TABLE [dbo].[Brands] ADD [Description] NVARCHAR(500) NOT NULL
                    CONSTRAINT [DF_Brands_Description_Legacy] DEFAULT N'';
        END
        """;
}
