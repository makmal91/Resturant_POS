using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSSystem.Application.Common.Interfaces;
using POSSystem.Infrastructure.Data;

namespace POSSystem.Infrastructure.Services;

public class ExceptionLogService : IExceptionLogService
{
    private readonly IDbContextFactory<POSDbContext> _contextFactory;
    private readonly ILogger<ExceptionLogService> _logger;

    public ExceptionLogService(
        IDbContextFactory<POSDbContext> contextFactory,
        ILogger<ExceptionLogService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public Task LogAsync(
        Exception ex,
        long? userId,
        long? branchId,
        string? module,
        string? formName,
        string? actionName)
    {
        var entry = ExceptionLogPersister.CreateEntry(ex, userId, branchId, module, formName, actionName);
        return ExceptionLogPersister.TryWriteAsync(_contextFactory, entry, _logger);
    }
}
