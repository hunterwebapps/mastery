using System.Diagnostics;
using Serilog.Context;

namespace Mastery.Api.Middleware;

/// <summary>
/// Middleware that extracts or generates a correlation ID for request tracing.
/// The correlation ID is propagated through:
/// - X-Correlation-ID header (incoming and outgoing)
/// - Activity.Current for W3C distributed tracing
/// - Serilog LogContext for structured logging
/// - HttpContext.Items for downstream access
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Store in HttpContext.Items for downstream access
        context.Items[CorrelationIdItemKey] = correlationId;

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
            }
            return Task.CompletedTask;
        });

        // Push to Serilog LogContext for all logs within this request
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // 1. Try to get from incoming header
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        // 2. Try to get from Activity.Current (W3C trace context)
        var activity = Activity.Current;
        if (activity != null)
        {
            return activity.TraceId.ToString();
        }

        // 3. Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}
