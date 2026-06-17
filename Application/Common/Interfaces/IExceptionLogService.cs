namespace POSSystem.Application.Common.Interfaces;

public interface IExceptionLogService
{
    Task LogAsync(
        Exception ex,
        long? userId,
        long? branchId,
        string? module,
        string? formName,
        string? actionName);
}
