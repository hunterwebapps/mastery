using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Mastery.Api.Workers;

/// <summary>
/// Background service that periodically generates proactive recommendations
/// for users who have had data changes since the last run.
/// </summary>
public sealed class RecommendationBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundWorkerOptions _options;
    private readonly ILogger<RecommendationBackgroundWorker> _logger;

    public RecommendationBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundWorkerOptions> options,
        ILogger<RecommendationBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("RecommendationBackgroundWorker is disabled via configuration");
            return;
        }

        _logger.LogInformation(
            "RecommendationBackgroundWorker started, interval: {IntervalHours}h, maxUsersPerRun: {MaxUsers}",
            _options.IntervalHours, _options.MaxUsersPerRun);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in recommendation background worker cycle");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(_options.IntervalHours), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("RecommendationBackgroundWorker stopped");
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        using var outerScope = _scopeFactory.CreateScope();
        var historyRepo = outerScope.ServiceProvider.GetRequiredService<IRecommendationRunHistoryRepository>();
        var unitOfWork = outerScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dateTimeProvider = outerScope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        // 1. Determine the "since" cutoff from last completed run
        var lastRun = await historyRepo.GetLastCompletedAsync(stoppingToken);
        var since = lastRun?.CompletedAt ?? dateTimeProvider.UtcNow.AddHours(-_options.IntervalHours);

        _logger.LogInformation("Background worker cycle: checking for changes since {Since:u}", since);

        // 2. Find users with changes
        var changedUserIds = await historyRepo.GetUserIdsWithChangesSinceAsync(since, stoppingToken);

        if (changedUserIds.Count == 0)
        {
            _logger.LogInformation("Background worker cycle: no users with changes, skipping");
            return;
        }

        // Apply max users cap
        var usersToProcess = changedUserIds.Count > _options.MaxUsersPerRun
            ? changedUserIds.Take(_options.MaxUsersPerRun).ToList()
            : changedUserIds;

        _logger.LogInformation(
            "Background worker cycle: {ChangedCount} users with changes, processing {ProcessCount}",
            changedUserIds.Count, usersToProcess.Count);

        // 3. Create run history record
        var run = RecommendationRunHistory.Start(dateTimeProvider.UtcNow, changedUserIds.Count);
        await historyRepo.AddAsync(run, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        // 4. Process each user in their own scope
        foreach (var userId in usersToProcess)
        {
            if (stoppingToken.IsCancellationRequested) break;

            await ProcessUserAsync(userId, run, stoppingToken);
        }

        // 5. Complete the run (use a fresh scope to save since outer scope may have stale tracking)
        using var completionScope = _scopeFactory.CreateScope();
        var completionRepo = completionScope.ServiceProvider.GetRequiredService<IRecommendationRunHistoryRepository>();
        var completionUow = completionScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var runEntity = await completionRepo.GetByIdAsync(run.Id, stoppingToken);
        if (runEntity is not null)
        {
            runEntity.Complete();
            await completionUow.SaveChangesAsync(stoppingToken);
        }

        _logger.LogInformation(
            "Background worker cycle complete: {Processed} users processed, {Recs} recommendations generated, {Errors} errors",
            run.UsersProcessed, run.RecommendationsGenerated, run.Errors);
    }

    private async Task ProcessUserAsync(
        string userId,
        RecommendationRunHistory run,
        CancellationToken stoppingToken)
    {
        try
        {
            using var userScope = _scopeFactory.CreateScope();
            var pipeline = userScope.ServiceProvider.GetRequiredService<IRecommendationPipeline>();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(TimeSpan.FromMinutes(_options.TimeoutMinutesPerUser));

            var results = await pipeline.ExecuteAsync(
                userId,
                RecommendationContext.ProactiveCheck,
                cts.Token);

            run.RecordUserProcessed(results.Count);

            _logger.LogDebug(
                "Generated {Count} recommendations for user {UserId}",
                results.Count, userId);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            run.RecordError(userId, "Timed out");
            _logger.LogWarning(
                "Recommendation generation timed out for user {UserId} after {Timeout}m",
                userId, _options.TimeoutMinutesPerUser);
        }
        catch (Exception ex)
        {
            run.RecordError(userId, ex.Message);
            _logger.LogError(ex, "Failed to generate recommendations for user {UserId}", userId);
        }
    }
}
