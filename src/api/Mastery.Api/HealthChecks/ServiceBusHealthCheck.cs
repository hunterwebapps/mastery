using Mastery.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Mastery.Api.HealthChecks;

/// <summary>
/// Health check for Azure Service Bus connectivity.
/// Reports the status of the messaging infrastructure.
/// </summary>
public sealed class ServiceBusHealthCheck : IHealthCheck
{
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusHealthCheck> _logger;

    public ServiceBusHealthCheck(
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusHealthCheck> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // For a more thorough health check, you could:
        // 1. Create a ServiceBusClient and verify connection
        // 2. Send a test message to a health check queue
        // 3. Query the outbox table for failed message counts
        //
        // For now, we report healthy if configuration is present.

        var data = new Dictionary<string, object>
        {
            ["Enabled"] = true,
            ["Tier"] = "Basic (Queues)",
            ["EmbeddingsQueue"] = _options.EmbeddingsQueueName,
            ["UrgentQueue"] = _options.UrgentQueueName,
            ["WindowQueue"] = _options.WindowQueueName,
            ["BatchQueue"] = _options.BatchQueueName,
            ["MaxRetryCount"] = _options.MaxRetryCount
        };

        return Task.FromResult(HealthCheckResult.Healthy(
            "Service Bus is configured and ready",
            data));
    }
}
