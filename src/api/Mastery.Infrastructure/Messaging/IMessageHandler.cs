namespace Mastery.Infrastructure.Messaging;

/// <summary>
/// Marker interface for message handlers.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// The queue name this handler subscribes to.
    /// </summary>
    string QueueName { get; }
}

/// <summary>
/// Interface for handling messages of a specific type from Azure Service Bus.
/// </summary>
/// <typeparam name="TMessage">The message type to handle.</typeparam>
public interface IMessageHandler<in TMessage> : IMessageHandler
    where TMessage : class
{
    /// <summary>
    /// Handles the received message.
    /// </summary>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
