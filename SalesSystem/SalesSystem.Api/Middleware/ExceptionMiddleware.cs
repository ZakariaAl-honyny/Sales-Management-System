using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SalesSystem.Api.Middleware;

/// <summary>
/// Global exception handler middleware that catches unhandled exceptions and returns a consistent JSON error response.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        object errorResponse;

        if (exception is FluentValidation.ValidationException validationException)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            errorResponse = new
            {
                error = "Validation failed",
                errorCode = "VALIDATION_ERROR",
                details = validationException.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            };
        }
        else if (exception is System.UnauthorizedAccessException)
        {
            response.StatusCode = 403; // Forbidden
            errorResponse = new
            {
                error = "Unauthorized",
                errorCode = "UNAUTHORIZED_ERROR",
                details = "You do not have permission to access this resource"
            };
        }
        else if (IsDatabaseConnectionException(exception))
        {
            _logger.LogError(exception, "Database connection failure");
            response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            errorResponse = new
            {
                error = "تعذر الاتصال بقاعدة البيانات. يرجى التحقق من الاتصال والمحاولة مرة أخرى.",
                errorCode = "DATABASE_CONNECTION_ERROR",
                details = GetInnerMessage(exception)
            };
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception");
            response.StatusCode = StatusCodes.Status500InternalServerError;
            errorResponse = new
            {
                error = "An unexpected error occurred",
                errorCode = "INTERNAL_ERROR"
            };
        }

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }

    private static bool IsDatabaseConnectionException(Exception ex)
    {
        // Check for common database connection exception types
        if (ex is InvalidOperationException && 
            (ex.Message.Contains("Connection string", StringComparison.OrdinalIgnoreCase) ||
             ex.Message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase) ||
             ex.Message.Contains("provider", StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check inner exception recursively
        if (ex.InnerException != null)
            return IsDatabaseConnectionException(ex.InnerException);

        // Check by exception type name (avoids direct dependency on SQL Server packages)
        var typeName = ex.GetType().FullName ?? "";
        return typeName.Contains("SqlException", StringComparison.Ordinal) ||
               typeName.Contains("SqlClient", StringComparison.Ordinal) ||
               typeName.Contains("EntityException", StringComparison.Ordinal);
    }

    private static string GetInnerMessage(Exception ex)
    {
        return ex.InnerException?.Message ?? ex.Message;
    }
}