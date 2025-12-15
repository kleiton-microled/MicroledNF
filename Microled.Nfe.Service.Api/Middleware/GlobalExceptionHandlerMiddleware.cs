using System.Net;
using System.Text.Json;

namespace Microled.Nfe.Service.Api.Middleware;

/// <summary>
/// Global exception handler middleware
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An error occurred while processing your request.";

        // Customize status code and message based on exception type
        if (exception is ArgumentException || exception is ArgumentNullException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = exception.Message;
        }
        else if (exception is InvalidOperationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = exception.Message;
        }
        else if (exception is NotImplementedException)
        {
            statusCode = HttpStatusCode.NotImplemented;
            message = exception.Message;
        }

        var response = new
        {
            error = new
            {
                code = (int)statusCode,
                message = message,
                correlationId = correlationId,
                timestamp = DateTime.UtcNow
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}

