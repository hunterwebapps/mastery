using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Messaging.Services;

/// <summary>
/// Background service that processes messages from an Azure Service Bus queue.
/// Uses ServiceBusProcessor for efficient message receiving with auto-complete.
/// </summary>
public sealed class ServiceBusQueueProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusQueueProcessor> _logger;
    private readonly List<ServiceBusProcessor> _processors = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public ServiceBusQueueProcessor(
        IServiceScopeFactory scopeFactory,
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusQueueProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _serviceBusClient = serviceBusClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ServiceBusQueueProcessor starting...");

        // Create processors for each queue
        var queues = new[]
        {
            _options.EmbeddingsQueueName,
            _options.UrgentQueueName,
            _options.WindowQueueName,
            _options.BatchQueueName
        };

        foreach (var queueName in queues)
        {
            try
            {
                var processor = _serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false,
                    MaxConcurrentCalls = 4,
                    PrefetchCount = 10
                });

                processor.ProcessMessageAsync += async args =>
                {
                    await ProcessMessageAsync(queueName, args, stoppingToken);
                };

                processor.ProcessErrorAsync += args =>
                {
                    _logger.LogError(args.Exception,
                        "Error processing message from queue {QueueName}: {ErrorSource}",
                        queueName, args.ErrorSource);
                    return Task.CompletedTask;
                };

                await processor.StartProcessingAsync(stoppingToken);
                _processors.Add(processor);

                _logger.LogInformation("Started processor for queue {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create processor for queue {QueueName}", queueName);
            }
        }

        // Keep running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    private async Task ProcessMessageAsync(
        string queueName,
        ProcessMessageEventArgs args,
        CancellationToken stoppingToken)
    {
        var messageId = args.Message.MessageId;
        var correlationId = args.Message.CorrelationId;

        try
        {
            _logger.LogDebug("Processing message {MessageId} from queue {QueueName}",
                messageId, queueName);

            // Get message type from application properties
            if (!args.Message.ApplicationProperties.TryGetValue("MessageType", out var messageTypeObj) ||
                messageTypeObj is not string messageTypeName)
            {
                _logger.LogWarning("Message {MessageId} missing MessageType property, dead-lettering",
                    messageId);
                await args.DeadLetterMessageAsync(args.Message, "MissingMessageType",
                    "MessageType application property is required", stoppingToken);
                return;
            }

            var messageType = Type.GetType(messageTypeName);
            if (messageType == null)
            {
                _logger.LogWarning("Unknown message type {MessageType} for message {MessageId}, dead-lettering",
                    messageTypeName, messageId);
                await args.DeadLetterMessageAsync(args.Message, "UnknownMessageType",
                    $"Could not resolve type: {messageTypeName}", stoppingToken);
                return;
            }

            // Deserialize the message
            var body = args.Message.Body.ToString();
            var message = JsonSerializer.Deserialize(body, messageType, _jsonOptions);

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message {MessageId}, dead-lettering", messageId);
                await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed",
                    "Message body could not be deserialized", stoppingToken);
                return;
            }

            // Find and invoke the handler
            using var scope = _scopeFactory.CreateScope();
            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
            var handlers = scope.ServiceProvider.GetServices(handlerType);

            var handled = false;
            foreach (var handler in handlers)
            {
                if (handler is IMessageHandler messageHandler && messageHandler.QueueName == queueName)
                {
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    await (Task)handleMethod!.Invoke(handler, [message, stoppingToken])!;
                    handled = true;
                }
            }

            if (!handled)
            {
                _logger.LogWarning("No handler found for message type {MessageType} on queue {QueueName}",
                    messageTypeName, queueName);
            }

            // Complete the message
            await args.CompleteMessageAsync(args.Message, stoppingToken);

            _logger.LogDebug("Completed message {MessageId} from queue {QueueName}",
                messageId, queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId} from queue {QueueName}",
                messageId, queueName);

            // Abandon to allow retry (up to Azure Service Bus's max delivery count)
            await args.AbandonMessageAsync(args.Message, cancellationToken: stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ServiceBusQueueProcessor stopping...");

        foreach (var processor in _processors)
        {
            try
            {
                await processor.StopProcessingAsync(cancellationToken);
                await processor.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping queue processor");
            }
        }

        _processors.Clear();

        await base.StopAsync(cancellationToken);
    }
}
