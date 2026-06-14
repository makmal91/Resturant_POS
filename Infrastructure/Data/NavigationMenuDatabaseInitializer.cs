using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class NavigationMenuDatabaseInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        const string createTableSql = """
            IF OBJECT_ID(N'[dbo].[Menus]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Menus] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(100) NOT NULL,
                    [Route] NVARCHAR(200) NULL,
                    [Icon] NVARCHAR(50) NULL,
                    [ModuleName] NVARCHAR(100) NULL,
                    [ParentId] INT NULL,
                    [DisplayOrder] INT NOT NULL,
                    [IsActive] BIT NOT NULL CONSTRAINT [DF_Menus_IsActive] DEFAULT 1,
                    CONSTRAINT [FK_Menus_Menus_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Menus]([Id])
                );
                CREATE INDEX [idx_menus_displayorder] ON [Menus]([DisplayOrder]);
                CREATE INDEX [idx_menus_parentid] ON [Menus]([ParentId]);
            END
            """;

        try
        {
            await context.Database.ExecuteSqlRawAsync(createTableSql);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Navigation menu schema batch skipped or partially applied.");
        }
    }
}
