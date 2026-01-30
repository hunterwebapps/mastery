namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the messaging infrastructure for publishing domain events.
/// Allows swapping between Service Bus and SQL-based outbox patterns.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to the specified topic.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="topicName">The topic/queue name to publish to.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(string topicName, T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Publishes a message with a delay (scheduled delivery).
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="topicName">The topic/queue name to publish to.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="delay">How long to delay before the message is delivered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishDelayedAsync<T>(string topicName, T message, TimeSpan delay, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Publishes a message scheduled for a specific time.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="topicName">The topic/queue name to publish to.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="scheduledTime">The UTC time when the message should be delivered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishScheduledAsync<T>(string topicName, T message, DateTime scheduledTime, CancellationToken cancellationToken = default)
        where T : class;
}
