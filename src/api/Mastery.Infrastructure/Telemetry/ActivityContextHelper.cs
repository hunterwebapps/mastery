using System.Diagnostics;

namespace Mastery.Infrastructure.Telemetry;

/// <summary>
/// Helper for creating Activity scopes linked to correlation IDs for distributed tracing.
/// </summary>
public static class ActivityContextHelper
{
    private static readonly ActivitySource ActivitySource = new("Mastery.Infrastructure.Messaging");

    /// <summary>
    /// Starts a new Activity linked to the original trace via correlation ID.
    /// Use this in message consumers to maintain distributed tracing context.
    /// </summary>
    /// <param name="operationName">The name of the operation being performed.</param>
    /// <param name="correlationId">Optional correlation ID from the message.</param>
    /// <returns>An Activity that should be disposed when the operation completes.</returns>
    public static Activity? StartLinkedActivity(string operationName, string? correlationId)
    {
        var activity = ActivitySource.StartActivity(operationName, ActivityKind.Consumer);

        if (activity != null && !string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag("correlation.id", correlationId);
        }

        return activity;
    }
}
