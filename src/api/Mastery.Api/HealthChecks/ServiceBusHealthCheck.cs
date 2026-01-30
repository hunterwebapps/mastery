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
        if (!_options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "Service Bus is disabled, using SQL-based outbox pattern"));
        }

        if (string.IsNullOrEmpty(_options.ConnectionString))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Service Bus is enabled but connection string is not configured"));
        }

        // For a more thorough health check, you could:
        // 1. Create a ServiceBusClient and verify connection
        // 2. Send a test message to a health check queue
        // 3. Query CAP's internal tables for failed message counts
        //
        // For now, we report healthy if configuration is present.
        // CAP handles actual connectivity and retries internally.

        var data = new Dictionary<string, object>
        {
            ["Enabled"] = true,
            ["Tier"] = "Basic (Queues)",
            ["EmbeddingsQueue"] = _options.EmbeddingsQueueName,
            ["UrgentQueue"] = _options.UrgentQueueName,
            ["WindowQueue"] = _options.WindowQueueName,
            ["BatchQueue"] = _options.BatchQueueName,
            ["MaxRetryCount"] = _options.MaxRetryCount,
            ["DashboardEnabled"] = _options.EnableDashboard
        };

        return Task.FromResult(HealthCheckResult.Healthy(
            "Service Bus is configured and ready",
            data));
    }
}
