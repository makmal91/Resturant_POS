using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Services;

internal static class ExceptionLogPersister
{
    public static async Task TryWriteAsync(
        IDbContextFactory<POSDbContext> contextFactory,
        ExceptionLog entry,
        ILogger logger)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            if (!await TableExistsAsync(context))
            {
                logger.LogWarning(
                    "ExceptionLogs table is missing. Apply EF migrations or run database bootstrap.");
                return;
            }

            context.ExceptionLogs.Add(entry);
            await context.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            logger.LogCritical(
                logEx,
                "Failed to persist exception log. Original error: {OriginalMessage}",
                entry.ExceptionMessage);
        }
    }

    public static ExceptionLog CreateEntry(
        Exception ex,
        long? userId,
        long? branchId,
        string? module,
        string? formName,
        string? actionName)
    {
        return new ExceptionLog
        {
            UserId = userId,
            BranchId = branchId,
            Module = string.IsNullOrWhiteSpace(module) ? "Unknown" : module.Trim(),
            FormName = string.IsNullOrWhiteSpace(formName) ? null : formName.Trim(),
            ActionName = string.IsNullOrWhiteSpace(actionName) ? null : actionName.Trim(),
            ExceptionMessage = FormatMessage(ex),
            StackTrace = ex.StackTrace,
            InnerException = FormatInnerChain(ex),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static string FormatMessage(Exception ex) =>
        ex switch
        {
            DbUpdateException dbUpdate when !string.IsNullOrWhiteSpace(dbUpdate.InnerException?.Message)
                => dbUpdate.InnerException!.Message,
            _ => ex.Message
        };

    private static string? FormatInnerChain(Exception ex)
    {
        var parts = new List<string>();
        var current = ex.InnerException;
        while (current != null)
        {
            parts.Add(current.Message);
            current = current.InnerException;
        }

        return parts.Count == 0 ? null : string.Join(" -> ", parts);
    }

    private static async Task<bool> TableExistsAsync(POSDbContext context)
    {
        var exists = await context.Database
            .SqlQueryRaw<int>(
                "SELECT CASE WHEN OBJECT_ID(N'dbo.ExceptionLogs', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]")
            .FirstOrDefaultAsync();
        return exists == 1;
    }
}
