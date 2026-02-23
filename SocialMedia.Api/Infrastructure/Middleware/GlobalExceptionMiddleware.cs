using Microsoft.AspNetCore.Mvc;

namespace SocialMedia.Api.Infrastructure.Middleware;

public class GlobalExceptionMiddleware
{
    private const string UnexpectedErrorTitle = "Unexpected server error";

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            string correlationId = context.GetCorrelationId();

            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started, exception middleware cannot write response.");
                throw;
            }

            ProblemDetails problem = new()
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = UnexpectedErrorTitle,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                Instance = context.Request.Path
            };

            if (_environment.IsDevelopment())
            {
                problem.Detail = exception.Message;
            }

            problem.Extensions["correlationId"] = correlationId;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
