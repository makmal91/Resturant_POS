using Microsoft.Extensions.Logging;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Domain;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Services;

public class ExceptionLogService : IExceptionLogService
{
    private readonly POSDbContext _context;
    private readonly ILogger<ExceptionLogService> _logger;

    public ExceptionLogService(POSDbContext context, ILogger<ExceptionLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        Exception ex,
        long? userId,
        long? branchId,
        string? module,
        string? formName,
        string? actionName)
    {
        try
        {
            var log = new ExceptionLog
            {
                UserId = userId,
                BranchId = branchId,
                Module = string.IsNullOrWhiteSpace(module) ? "Unknown" : module.Trim(),
                FormName = string.IsNullOrWhiteSpace(formName) ? null : formName.Trim(),
                ActionName = string.IsNullOrWhiteSpace(actionName) ? null : actionName.Trim(),
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException?.Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.ExceptionLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            _logger.LogCritical(
                logEx,
                "Failed to persist exception log. Original error: {OriginalMessage}. " +
                "Ensure EF migrations are applied (ExceptionLogs table).",
                ex.Message);
        }
    }
}
