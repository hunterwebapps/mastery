using Mastery.Application.Common.Interfaces;
using Mastery.Infrastructure.Outbox;
using Microsoft.Extensions.Options;

namespace Mastery.Api.Workers;

/// <summary>
/// Background service that processes outbox entries for embedding generation.
/// Uses lease-based batch processing with deduplication to support concurrent workers.
/// </summary>
public sealed class OutboxProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxWorkerOptions _options;
    private readonly ILogger<OutboxProcessingWorker> _logger;
    private readonly string _workerId;

    public OutboxProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxWorkerOptions> options,
        ILogger<OutboxProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("OutboxProcessingWorker is disabled via configuration");
            return;
        }

        _logger.LogInformation(
            "OutboxProcessingWorker started (id: {WorkerId}), polling interval: {PollingMs}ms, batch size: {BatchSize}",
            _workerId,
            _options.PollingIntervalMs,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in outbox processing worker cycle");
            }

            try
            {
                await Task.Delay(_options.PollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("OutboxProcessingWorker stopped");
    }

    private async Task ProcessCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var embeddingProcessor = scope.ServiceProvider.GetRequiredService<IEmbeddingProcessor>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        // First, release any expired leases from crashed workers
        await outboxRepository.ReleaseExpiredLeasesAsync(stoppingToken);

        // Calculate lease expiration
        var leaseUntil = dateTimeProvider.UtcNow.AddMinutes(_options.LeaseMinutes);

        // Acquire a batch of entries
        var entries = await outboxRepository.AcquireBatchAsync(
            _workerId,
            leaseUntil,
            _options.BatchSize,
            _options.MaxRetries,
            stoppingToken);

        if (entries.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Acquired {Count} outbox entries for processing", entries.Count);

        // Deduplicate: keep only the latest entry per (EntityType, EntityId)
        var deduplicated = entries
            .GroupBy(e => (e.EntityType, e.EntityId))
            .Select(g => g.OrderByDescending(e => e.CreatedAt).First())
            .ToList();

        if (deduplicated.Count < entries.Count)
        {
            _logger.LogDebug(
                "Deduplicated {Original} entries to {Deduped} unique entities",
                entries.Count,
                deduplicated.Count);
        }

        try
        {
            // Process the deduplicated batch
            await embeddingProcessor.ProcessBatchAsync(deduplicated, stoppingToken);

            // Mark ALL original entries (including duplicates) as processed
            foreach (var entry in entries)
            {
                entry.MarkProcessed(dateTimeProvider.UtcNow);
            }

            _logger.LogInformation(
                "Successfully processed {Count} outbox entries ({Unique} unique entities)",
                entries.Count,
                deduplicated.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox batch");

            // Mark all entries as failed
            foreach (var entry in entries)
            {
                entry.MarkFailed(ex.Message, _options.MaxRetries);
            }
        }

        // Save the updated entries
        await outboxRepository.UpdateBatchAsync(entries, stoppingToken);
    }
}
