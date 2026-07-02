using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

public static class PartyLedgerInitializer
{
    public static async Task EnsureSchemaAsync(POSDbContext context, ILogger logger)
    {
        var batches = new[]
        {
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleInvoices]') AND name = N'IsCreditSale')
                ALTER TABLE [dbo].[SaleInvoices]
                    ADD [IsCreditSale] BIT NOT NULL CONSTRAINT [DF_SaleInvoices_IsCreditSale] DEFAULT 0;
            """,
            """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Purchases]') AND name = N'IsCreditPurchase')
                ALTER TABLE [dbo].[Purchases]
                    ADD [IsCreditPurchase] BIT NOT NULL CONSTRAINT [DF_Purchases_IsCreditPurchase] DEFAULT 0;
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
                logger.LogWarning(ex, "PartyLedger schema batch skipped or partially applied.");
            }
        }
    }
}
