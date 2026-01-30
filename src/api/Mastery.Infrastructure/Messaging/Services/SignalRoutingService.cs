using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Messaging.Services;

/// <summary>
/// Service for routing signals to the appropriate Service Bus topic based on priority.
/// </summary>
public sealed class SignalRoutingService(
    IMessageBus _messageBus,
    IOptions<ServiceBusOptions> _options,
    IUserScheduleResolver _scheduleResolver,
    ILogger<SignalRoutingService> _logger)
{
    /// <summary>
    /// Routes a batch of signals for a single user to the appropriate queue.
    /// All signals in the batch should have the same priority.
    /// </summary>
    public async Task RouteSignalsAsync(
        IReadOnlyList<SignalClassification> classifications,
        SignalPriority priority,
        string userId,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (classifications.Count == 0)
        {
            return;
        }

        // All classifications should have the same priority (grouped by caller)
        var queueName = GetQueueForPriority(priority);

        // Convert classifications to signal events
        var signals = classifications.Select(c => new SignalRoutedEvent
        {
            UserId = userId,
            EventType = c.EventType,
            Priority = c.Priority,
            WindowType = c.WindowType,
            TargetEntityType = c.TargetEntityType,
            TargetEntityId = c.TargetEntityId,
            CorrelationId = correlationId
        }).ToList();

        // For window-aligned signals, get the scheduled time
        if (priority == SignalPriority.WindowAligned)
        {
            var windowType = classifications[0].WindowType;
            var windowStart = await _scheduleResolver.GetNextWindowStartAsync(userId, windowType, cancellationToken);

            // Update all signals with scheduled window start
            signals = signals.Select(s => s with { ScheduledWindowStart = windowStart }).ToList();

            var batch = new SignalRoutedBatchEvent
            {
                UserId = userId,
                Signals = signals,
                CorrelationId = correlationId
            };

            if (windowStart > DateTime.UtcNow)
            {
                await _messageBus.PublishScheduledAsync(queueName, batch, windowStart, cancellationToken);
                _logger.LogDebug(
                    "Scheduled batch of {Count} window signals for user {UserId} to {Queue} at {WindowStart}",
                    signals.Count, userId, queueName, windowStart);
            }
            else
            {
                await _messageBus.PublishAsync(queueName, batch, cancellationToken);
                _logger.LogDebug(
                    "Routed batch of {Count} window signals for user {UserId} to {Queue} immediately",
                    signals.Count, userId, queueName);
            }
        }
        else
        {
            // Urgent, Standard, and Low priority signals are published immediately as batch
            var batch = new SignalRoutedBatchEvent
            {
                UserId = userId,
                Signals = signals,
                CorrelationId = correlationId
            };

            await _messageBus.PublishAsync(queueName, batch, cancellationToken);
            _logger.LogDebug(
                "Routed batch of {Count} {Priority} signals for user {UserId} to {Queue}",
                signals.Count, priority, userId, queueName);
        }
    }

    private string GetQueueForPriority(SignalPriority priority) => priority switch
    {
        SignalPriority.Urgent => _options.Value.UrgentQueueName,
        SignalPriority.WindowAligned => _options.Value.WindowQueueName,
        SignalPriority.Standard or SignalPriority.Low => _options.Value.BatchQueueName,
        _ => _options.Value.BatchQueueName
    };
}
