using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using POSSystem.Application.Common.Interfaces;

namespace POSSystem.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IExceptionLogService exceptionLogService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            if (context.Items.ContainsKey("ExceptionLogged") != true)
            {
                await exceptionLogService.LogAsync(
                    ex,
                    GetUserId(context),
                    GetBranchId(context),
                    context.Request.Headers["x-module"].FirstOrDefault(),
                    context.Request.Headers["x-form"].FirstOrDefault(),
                    context.Request.Headers["x-action"].FirstOrDefault());
            }
            await HandleExceptionAsync(context, ex);
        }
    }

    private static long? GetUserId(HttpContext context)
    {
        var value =
            context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            context.User?.FindFirstValue("userId") ??
            context.User?.FindFirstValue("UserId");

        return long.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;
    }

    private static long? GetBranchId(HttpContext context)
    {
        var headerValue = context.Request.Headers["X-Branch-Id"].FirstOrDefault();
        if (long.TryParse(headerValue, out var headerBranchId) && headerBranchId > 0)
            return headerBranchId;

        var claimValue =
            context.User?.FindFirstValue("branchId") ??
            context.User?.FindFirstValue("BranchId");

        return long.TryParse(claimValue, out var parsed) && parsed > 0 ? parsed : null;
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            InvalidOperationException => HttpStatusCode.BadRequest,
            BadHttpRequestException => HttpStatusCode.BadRequest,
            InvalidDataException => HttpStatusCode.BadRequest,
            DbUpdateException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var message = statusCode switch
        {
            HttpStatusCode.InternalServerError => "Internal Server Error",
            HttpStatusCode.BadRequest when exception is DbUpdateException dbUpdate
                && dbUpdate.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
                => "A record with the same value already exists.",
            HttpStatusCode.BadRequest when exception is DbUpdateException dbUpdate
                && !string.IsNullOrWhiteSpace(dbUpdate.InnerException?.Message)
                => dbUpdate.InnerException!.Message,
            _ => exception.Message
        };

        var response = new
        {
            statusCode = context.Response.StatusCode,
            message,
            details = exception.Message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
