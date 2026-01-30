using System.Text.Json;
using DotNetCore.CAP;
using Mastery.Application.Common.Interfaces;
using Mastery.Infrastructure.Messaging.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mastery.Infrastructure.Messaging;

/// <summary>
/// Dependency injection extension for CAP + Azure Service Bus messaging.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds CAP messaging services with Azure Service Bus transport.
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

        // Register the message bus abstraction
        services.AddScoped<IMessageBus, CapMessageBus>();

        // Register signal routing service
        services.AddScoped<SignalRoutingService>();

        // Register DLQ monitor as singleton (so health check can access it)
        // and also as hosted service (so it runs in the background)
        services.AddSingleton<DlqMonitorService>();
        services.AddHostedService(sp => sp.GetRequiredService<DlqMonitorService>());

        // Get connection string for CAP storage
        var sqlConnectionString = configuration.GetConnectionString("MasteryDb")
            ?? throw new InvalidOperationException("MasteryDb connection string is required for CAP");

        services.AddCap(capOptions =>
        {
            // Use SQL Server for CAP's outbox pattern
            capOptions.UseSqlServer(sqlOptions =>
            {
                sqlOptions.ConnectionString = sqlConnectionString;
                sqlOptions.Schema = "cap";
            });

            // Use Azure Service Bus as transport (Basic tier - queues only)
            capOptions.UseAzureServiceBus(sbOptions =>
            {
                sbOptions.ConnectionString = options.ConnectionString;
                // Basic tier uses queues, not topics - no TopicPath needed
                sbOptions.EnableSessions = false;
            });

            // Retry configuration
            capOptions.FailedRetryCount = options.MaxRetryCount;
            capOptions.FailedRetryInterval = options.FailedRetryIntervalSeconds;

            // Consumer group for subscription naming
            capOptions.ConsumerThreadCount = 4;
            capOptions.DefaultGroupName = options.ConsumerGroup;

            // Use JSON serialization
            capOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        return services;
    }
}
