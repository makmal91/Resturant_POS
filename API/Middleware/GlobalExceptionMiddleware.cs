using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
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