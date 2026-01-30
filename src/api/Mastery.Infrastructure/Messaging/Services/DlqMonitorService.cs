using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Messaging.Services;

/// <summary>
/// Status of a single topic in the DLQ.
/// </summary>
public record DlqTopicStatus(string TopicName, int FailedCount, string TableSource);

/// <summary>
/// Aggregated DLQ health status across all monitored tables.
/// </summary>
public record DlqHealthStatus(IReadOnlyList<DlqTopicStatus> Topics, DateTimeOffset CheckedAt);

/// <summary>
/// Background service that monitors dead-letter queues and alerts when thresholds are exceeded.
/// CAP automatically handles DLQ via its failed message table, which is exposed via the dashboard.
/// This service provides additional monitoring and alerting capabilities.
/// </summary>
public sealed class DlqMonitorService : BackgroundService
{
    private readonly ServiceBusOptions _options;
    private readonly ILogger<DlqMonitorService> _logger;
    private readonly string _connectionString;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    // Thresholds for alerting
    private const int WarningThreshold = 50;
    private const int CriticalThreshold = 100;

    // Cached status for health check access
    private DlqHealthStatus _lastStatus = new([], DateTimeOffset.MinValue);
    private readonly object _statusLock = new();

    public DlqMonitorService(
        IOptions<ServiceBusOptions> options,
        IConfiguration configuration,
        ILogger<DlqMonitorService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("MasteryDb")
            ?? throw new InvalidOperationException("MasteryDb connection string is required for DLQ monitoring");
    }

    /// <summary>
    /// Gets the current DLQ status. Used by the health check.
    /// </summary>
    public DlqHealthStatus GetCurrentStatus()
    {
        lock (_statusLock)
        {
            return _lastStatus;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DLQ Monitor started, checking every {Interval} minutes",
            _checkInterval.TotalMinutes);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDlqStatusAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking DLQ status");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("DLQ Monitor stopped");
    }

    private async Task CheckDlqStatusAsync(CancellationToken ct)
    {
        var topics = new List<DlqTopicStatus>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        // Query failed messages from cap.Received (inbound messages)
        var receivedFailures = await QueryFailedMessagesAsync(
            connection,
            "cap.Received",
            ct);
        topics.AddRange(receivedFailures);

        // Query failed messages from cap.Published (outbound messages)
        var publishedFailures = await QueryFailedMessagesAsync(
            connection,
            "cap.Published",
            ct);
        topics.AddRange(publishedFailures);

        // Update cached status
        var status = new DlqHealthStatus(topics.AsReadOnly(), DateTimeOffset.UtcNow);
        lock (_statusLock)
        {
            _lastStatus = status;
        }

        // Log alerts for topics exceeding thresholds
        foreach (var topic in topics)
        {
            LogDlqAlert(topic.TopicName, topic.FailedCount, topic.TableSource);
        }

        var totalFailed = topics.Sum(t => t.FailedCount);
        if (totalFailed == 0)
        {
            _logger.LogDebug("DLQ health check completed - no failed messages");
        }
        else
        {
            _logger.LogInformation(
                "DLQ health check completed - {TotalFailed} failed messages across {TopicCount} topics",
                totalFailed,
                topics.Count);
        }
    }

    private async Task<List<DlqTopicStatus>> QueryFailedMessagesAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken ct)
    {
        var results = new List<DlqTopicStatus>();

        // Query to get failed message counts grouped by topic name
        var query = $"""
            SELECT [Name], COUNT(*) AS FailedCount
            FROM {tableName}
            WHERE StatusName = 'Failed'
            GROUP BY [Name]
            HAVING COUNT(*) > 0
            """;

        try
        {
            await using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var topicName = reader.GetString(0);
                var failedCount = reader.GetInt32(1);
                results.Add(new DlqTopicStatus(topicName, failedCount, tableName));
            }
        }
        catch (SqlException ex) when (ex.Number == 208) // Invalid object name - table doesn't exist yet
        {
            _logger.LogDebug(
                "CAP table {TableName} does not exist yet - skipping DLQ check",
                tableName);
        }

        return results;
    }

    /// <summary>
    /// Records a DLQ alert for a specific topic.
    /// </summary>
    private void LogDlqAlert(string topicName, int messageCount, string tableSource)
    {
        if (messageCount >= CriticalThreshold)
        {
            _logger.LogCritical(
                "CRITICAL: DLQ for topic {Topic} in {Table} has {Count} messages (threshold: {Threshold})",
                topicName, tableSource, messageCount, CriticalThreshold);
        }
        else if (messageCount >= WarningThreshold)
        {
            _logger.LogWarning(
                "WARNING: DLQ for topic {Topic} in {Table} has {Count} messages (threshold: {Threshold})",
                topicName, tableSource, messageCount, WarningThreshold);
        }
    }
}
