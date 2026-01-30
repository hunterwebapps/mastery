using Mastery.Infrastructure.Messaging.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mastery.Api.HealthChecks;

/// <summary>
/// Health check for DLQ (dead-letter queue) status.
/// Reports unhealthy when failed message counts exceed critical thresholds.
/// </summary>
public sealed class DlqHealthCheck : IHealthCheck
{
    private readonly DlqMonitorService _monitor;
    private readonly ILogger<DlqHealthCheck> _logger;

    // Must match thresholds in DlqMonitorService
    private const int WarningThreshold = 50;
    private const int CriticalThreshold = 100;

    // Status is considered stale if older than 2x the check interval (30 min)
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(30);

    public DlqHealthCheck(
        DlqMonitorService monitor,
        ILogger<DlqHealthCheck> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = _monitor.GetCurrentStatus();

        // Check if we have recent data
        var staleness = DateTimeOffset.UtcNow - status.CheckedAt;
        if (staleness > StaleThreshold)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"DLQ status is stale (last checked {staleness.TotalMinutes:F0} minutes ago)",
                data: new Dictionary<string, object>
                {
                    ["LastCheckedAt"] = status.CheckedAt,
                    ["StalenessMinutes"] = staleness.TotalMinutes
                }));
        }

        var criticalTopics = status.Topics
            .Where(t => t.FailedCount >= CriticalThreshold)
            .ToList();

        var warningTopics = status.Topics
            .Where(t => t.FailedCount >= WarningThreshold && t.FailedCount < CriticalThreshold)
            .ToList();

        var data = new Dictionary<string, object>
        {
            ["LastCheckedAt"] = status.CheckedAt,
            ["TotalFailedMessages"] = status.Topics.Sum(t => t.FailedCount),
            ["TopicsWithFailures"] = status.Topics.Count,
            ["CriticalTopics"] = criticalTopics.Count,
            ["WarningTopics"] = warningTopics.Count
        };

        if (criticalTopics.Count > 0)
        {
            var description = $"Critical DLQ backlog: {string.Join(", ", criticalTopics.Select(t => $"{t.TopicName}={t.FailedCount}"))}";

            foreach (var topic in criticalTopics)
            {
                data[$"Critical_{topic.TopicName}"] = new { topic.FailedCount, topic.TableSource };
            }

            return Task.FromResult(HealthCheckResult.Unhealthy(description, data: data));
        }

        if (warningTopics.Count > 0)
        {
            var description = $"Warning DLQ backlog: {string.Join(", ", warningTopics.Select(t => $"{t.TopicName}={t.FailedCount}"))}";

            foreach (var topic in warningTopics)
            {
                data[$"Warning_{topic.TopicName}"] = new { topic.FailedCount, topic.TableSource };
            }

            return Task.FromResult(HealthCheckResult.Degraded(description, data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "No DLQ backlog detected",
            data: data));
    }
}
