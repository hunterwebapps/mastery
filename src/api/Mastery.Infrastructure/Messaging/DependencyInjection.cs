using Azure.Messaging.ServiceBus;
using Mastery.Application.Common.Interfaces;
using Mastery.Infrastructure.Messaging.Consumers;
using Mastery.Infrastructure.Messaging.Events;
using Mastery.Infrastructure.Messaging.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mastery.Infrastructure.Messaging;

/// <summary>
/// Dependency injection extension for Azure Service Bus messaging with transactional outbox.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Azure Service Bus messaging services with transactional outbox pattern.
    /// </summary>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register options
        services.Configure<ServiceBusOptions>(
            configuration.GetSection(ServiceBusOptions.SectionName));

        var options = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? new ServiceBusOptions();

        // Register Azure Service Bus client
        services.AddSingleton(_ => new ServiceBusClient(options.ConnectionString));

        // Register the message bus abstraction (writes to outbox table)
        services.AddScoped<IMessageBus, DirectServiceBusMessageBus>();

        // Register signal routing service
        services.AddScoped<SignalRoutingService>();

        // Register DLQ monitor as singleton (so health check can access it)
        // and also as hosted service (so it runs in the background)
        services.AddSingleton<DlqMonitorService>();
        services.AddHostedService(sp => sp.GetRequiredService<DlqMonitorService>());

        // Register window signal scheduler as singleton and hosted service
        // Emits MorningWindowStart/EveningWindowStart signals at appropriate times per user
        services.AddSingleton<WindowSignalSchedulerService>();
        services.AddHostedService(sp => sp.GetRequiredService<WindowSignalSchedulerService>());

        // Register queue processor (receives messages from Azure Service Bus)
        services.AddHostedService<ServiceBusQueueProcessor>();

        // Register message handlers
        services.AddScoped<IMessageHandler<EntityChangedBatchEvent>, EmbeddingConsumer>();
        services.AddScoped<IMessageHandler<SignalRoutedBatchEvent>, UrgentSignalConsumer>();
        services.AddScoped<IMessageHandler<SignalRoutedBatchEvent>, WindowSignalConsumer>();
        services.AddScoped<IMessageHandler<SignalRoutedBatchEvent>, BatchSignalConsumer>();

        return services;
    }
}
