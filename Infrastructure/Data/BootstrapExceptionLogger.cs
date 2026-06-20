using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Domain;

namespace POSSystem.Infrastructure.Data;

public static class BootstrapExceptionLogger
{
    public static async Task LogAsync(
        POSDbContext context,
        ILogger logger,
        Exception ex,
        string module,
        string? formName = null,
        string? actionName = null)
    {
        logger.LogError(ex, "{Module} failed.", module);

        try
        {
            if (!await ExceptionLogsTableExistsAsync(context))
                return;

            context.ExceptionLogs.Add(new ExceptionLog
            {
                Module = string.IsNullOrWhiteSpace(module) ? "Startup" : module.Trim(),
                FormName = formName,
                ActionName = actionName,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException?.Message,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            logger.LogError(logEx, "Failed to persist startup exception log for {Module}.", module);
        }
    }

    private static async Task<bool> ExceptionLogsTableExistsAsync(POSDbContext context)
    {
        var exists = await context.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.ExceptionLogs', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]")
            .FirstOrDefaultAsync();
        return exists == 1;
    }
}
