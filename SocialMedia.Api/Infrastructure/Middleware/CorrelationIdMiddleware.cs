using Microsoft.Extensions.Primitives;

namespace SocialMedia.Api.Infrastructure.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = ResolveCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Items[ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out StringValues headerValues))
        {
            string? incomingCorrelationId = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(incomingCorrelationId))
            {
                return incomingCorrelationId.Trim();
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
