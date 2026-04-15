using System.Net;
using System.Text.Json;
using FluentValidation;

namespace DndCampaignManager.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        var (statusCode, message) = exception switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, new
            {
                Type = "ValidationError",
                Errors = ve.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            } as object),

            KeyNotFoundException knf => (HttpStatusCode.NotFound, new
            {
                Type = "NotFound",
                Message = knf.Message
            } as object),

            UnauthorizedAccessException ua => (HttpStatusCode.Forbidden, new
            {
                Type = "Forbidden",
                Message = ua.Message
            } as object),

            InvalidOperationException io => (HttpStatusCode.Conflict, new
            {
                Type = "Conflict",
                Message = io.Message
            } as object),

            _ => (HttpStatusCode.InternalServerError, new
            {
                Type = "ServerError",
                Message = "An unexpected error occurred"
            } as object)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
