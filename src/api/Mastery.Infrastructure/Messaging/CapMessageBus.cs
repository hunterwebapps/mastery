using DotNetCore.CAP;
using Mastery.Application.Common.Interfaces;

namespace Mastery.Infrastructure.Messaging;

/// <summary>
/// CAP-based implementation of the message bus abstraction.
/// </summary>
public sealed class CapMessageBus(ICapPublisher _capPublisher) : IMessageBus
{
    public async Task PublishAsync<T>(string topicName, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        await _capPublisher.PublishAsync(topicName, message, cancellationToken: cancellationToken);
    }

    public async Task PublishDelayedAsync<T>(string topicName, T message, TimeSpan delay, CancellationToken cancellationToken = default)
        where T : class
    {
        // CAP supports delayed messages via the headers
        var headers = new Dictionary<string, string?>
        {
            ["cap-delay"] = ((long)delay.TotalSeconds).ToString()
        };

        await _capPublisher.PublishAsync(topicName, message, headers, cancellationToken);
    }

    public async Task PublishScheduledAsync<T>(string topicName, T message, DateTime scheduledTime, CancellationToken cancellationToken = default)
        where T : class
    {
        var now = DateTime.UtcNow;
        var delay = scheduledTime > now ? scheduledTime - now : TimeSpan.Zero;

        await PublishDelayedAsync(topicName, message, delay, cancellationToken);
    }
}
