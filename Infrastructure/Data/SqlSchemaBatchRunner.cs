using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace POSSystem.Infrastructure.Data;

internal static class SqlSchemaBatchRunner
{
    public static async Task ExecuteAsync(
        POSDbContext context,
        ILogger logger,
        string module,
        IEnumerable<string> batches)
    {
        foreach (var batch in batches)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(batch);
            }
            catch (Exception ex)
            {
                await BootstrapExceptionLogger.LogAsync(
                    context,
                    logger,
                    ex,
                    module,
                    actionName: "SchemaPatch");
            }
        }
    }
}
