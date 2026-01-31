using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Mastery.Application.Common.Interfaces;

namespace Mastery.Infrastructure.Messaging;

/// <summary>
/// Message bus implementation that sends messages directly to Azure Service Bus queues.
/// </summary>
public sealed class DirectServiceBusMessageBus : IMessageBus, IDisposable
{
    private readonly ServiceBusClient _client;
    private readonly Dictionary<string, ServiceBusSender> _senders = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Lock _lock = new();

    public DirectServiceBusMessageBus(ServiceBusClient client)
    {
        _client = client;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        var sender = GetOrCreateSender(queueName);

        var serviceBusMessage = CreateMessage(message);
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async Task PublishDelayedAsync<T>(string queueName, T message, TimeSpan delay, CancellationToken cancellationToken = default)
        where T : class
    {
        var sender = GetOrCreateSender(queueName);

        var serviceBusMessage = CreateMessage(message);
        serviceBusMessage.ScheduledEnqueueTime = DateTimeOffset.UtcNow.Add(delay);

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async Task PublishScheduledAsync<T>(string queueName, T message, DateTime scheduledTime, CancellationToken cancellationToken = default)
        where T : class
    {
        var sender = GetOrCreateSender(queueName);

        var serviceBusMessage = CreateMessage(message);
        serviceBusMessage.ScheduledEnqueueTime = scheduledTime;

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    private ServiceBusMessage CreateMessage<T>(T message) where T : class
    {
        var payload = JsonSerializer.Serialize(message, _jsonOptions);

        var serviceBusMessage = new ServiceBusMessage(payload)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = Activity.Current?.Id
        };

        serviceBusMessage.ApplicationProperties["MessageType"] = typeof(T).AssemblyQualifiedName;

        return serviceBusMessage;
    }

    private ServiceBusSender GetOrCreateSender(string queueName)
    {
        lock (_lock)
        {
            if (!_senders.TryGetValue(queueName, out var sender))
            {
                sender = _client.CreateSender(queueName);
                _senders[queueName] = sender;
            }
            return sender;
        }
    }

    public void Dispose()
    {
        foreach (var sender in _senders.Values)
        {
            sender.DisposeAsync().GetAwaiter().GetResult();
        }
        _senders.Clear();
    }
}
