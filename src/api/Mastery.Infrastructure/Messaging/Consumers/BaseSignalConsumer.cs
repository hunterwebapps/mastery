using System.Text.Json;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Messaging.Events;
using Mastery.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Mastery.Infrastructure.Messaging.Consumers;

/// <summary>
/// Base class for signal batch consumers that provides the common processing pipeline.
/// Subclasses specify the queue subscription and window type determination.
/// </summary>
public abstract class BaseSignalConsumer<TConsumer>(
    string queueName,
    IUserStateAssembler stateAssembler,
    ITieredAssessmentEngine tieredEngine,
    ISignalEntryRepository signalEntryRepository,
    ISignalProcessingHistoryRepository historyRepo,
    IRecommendationRepository recommendationRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<TConsumer> logger) : IMessageHandler<SignalRoutedBatchEvent>
{
    public string QueueName { get; } = queueName;

    protected readonly IUserStateAssembler StateAssembler = stateAssembler;
    protected readonly ITieredAssessmentEngine TieredEngine = tieredEngine;
    protected readonly ISignalEntryRepository SignalEntryRepository = signalEntryRepository;
    protected readonly ISignalProcessingHistoryRepository HistoryRepo = historyRepo;
    protected readonly IRecommendationRepository RecommendationRepo = recommendationRepo;
    protected readonly IUnitOfWork UnitOfWork = unitOfWork;
    protected readonly IDateTimeProvider DateTimeProvider = dateTimeProvider;
    protected readonly ILogger<TConsumer> Logger = logger;

    /// <summary>
    /// Determines the processing window type for the given batch.
    /// </summary>
    protected abstract ProcessingWindowType GetWindowType(SignalRoutedBatchEvent batch);

    /// <summary>
    /// Whether this consumer should include scheduled window start times when creating signal entries.
    /// </summary>
    protected virtual bool IncludeScheduledWindowStart => false;

    /// <summary>
    /// The log level to use for successful processing completion.
    /// </summary>
    protected virtual LogLevel CompletionLogLevel => LogLevel.Information;

    /// <summary>
    /// Gets a description of the signal type for logging purposes.
    /// </summary>
    protected abstract string GetSignalTypeDescription(SignalRoutedBatchEvent batch);

    /// <summary>
    /// Handles the received message from Azure Service Bus.
    /// </summary>
    public async Task HandleAsync(SignalRoutedBatchEvent batch, CancellationToken cancellationToken)
    {
        using var activity = ActivityContextHelper.StartLinkedActivity(
            $"Process{GetSignalTypeDescription(batch)}Batch",
            batch.CorrelationId);

        using var prop1 = LogContext.PushProperty("CorrelationId", batch.CorrelationId ?? "unknown");
        using var prop2 = LogContext.PushProperty("BatchId", batch.BatchId);

        // IDEMPOTENCY CHECK: Skip if already processed
        var alreadyProcessed = await HistoryRepo.ExistsByBatchIdAsync(batch.BatchId, cancellationToken);
        if (alreadyProcessed)
        {
            Logger.LogInformation(
                "Batch {BatchId} for user {UserId} already processed, skipping (idempotent)",
                batch.BatchId, batch.UserId);
            return;
        }

        var signalDescription = GetSignalTypeDescription(batch);

        Logger.LogDebug(
            "Processing batch of {Count} {SignalType} signals for user {UserId}",
            batch.Signals.Count, signalDescription, batch.UserId);

        var startedAt = DateTimeProvider.UtcNow;
        var windowType = this.GetWindowType(batch);

        // Create SignalEntry instances for audit purposes
        var signals = CreateSignalEntries(batch);

        // Start tracking this processing cycle
        var history = SignalProcessingHistory.Start(
            batch.UserId,
            windowType,
            startedAt,
            signalsReceived: batch.Signals.Count,
            batchId: batch.BatchId);

        try
        {
            // 1. Assemble user state once for the batch
            var state = await StateAssembler.AssembleAsync(batch.UserId, cancellationToken);

            // 2. Run tiered assessment with all signals
            var outcome = await TieredEngine.AssessAsync(state, signals, cancellationToken);

            // 3. Record tier results
            history.RecordTier0Results(outcome.Statistics.Tier0RulesTriggered);
            if (outcome.Tier1Result != null)
            {
                history.RecordTier1Results(
                    outcome.Tier1Result.CombinedScore,
                    JsonSerializer.Serialize(outcome.Tier1Result.DeltaSummary));
            }
            if (outcome.Tier2Executed)
            {
                history.RecordTier2Executed();
            }

            // 4. Persist recommendations (with deduplication)
            var recommendationIds = new List<Guid>();
            var skippedDuplicates = 0;
            foreach (var recommendation in outcome.GeneratedRecommendations)
            {
                // Check for existing pending recommendation with same type and target
                var exists = await RecommendationRepo.ExistsPendingForTargetAsync(
                    recommendation.UserId,
                    recommendation.Type,
                    recommendation.Target.Kind,
                    recommendation.Target.EntityId,
                    cancellationToken);

                if (exists)
                {
                    skippedDuplicates++;
                    Logger.LogDebug(
                        "Skipped duplicate recommendation: {Type} for {TargetKind} {TargetEntityId}",
                        recommendation.Type, recommendation.Target.Kind, recommendation.Target.EntityId);
                    continue;
                }

                await RecommendationRepo.AddAsync(recommendation, cancellationToken);
                recommendationIds.Add(recommendation.Id);
            }

            if (skippedDuplicates > 0)
            {
                Logger.LogInformation(
                    "Skipped {Count} duplicate recommendations for user {UserId}",
                    skippedDuplicates, batch.UserId);
            }

            history.RecordRecommendations(recommendationIds.Count, recommendationIds);

            // 5. Mark signals as processed and persist for audit
            var now = DateTimeProvider.UtcNow;
            foreach (var signal in signals)
            {
                signal.MarkProcessed(now, outcome.FinalTier);
            }
            await SignalEntryRepository.AddRangeAsync(signals, cancellationToken);

            // 6. Complete history and save
            history.RecordOutcome(signals.Count, 0);
            history.Complete(DateTimeProvider.UtcNow);
            await HistoryRepo.AddAsync(history, cancellationToken);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            Logger.Log(
                CompletionLogLevel,
                "Processed batch of {Count} {SignalType} signals for user {UserId}, generated {RecCount} recommendations via {Tier}",
                batch.Signals.Count, signalDescription, batch.UserId, outcome.GeneratedRecommendations.Count, outcome.FinalTier);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing {SignalType} signal batch for user {UserId}", signalDescription, batch.UserId);

            history.RecordError(ex.Message);
            history.Complete(DateTimeProvider.UtcNow);

            try
            {
                await HistoryRepo.AddAsync(history, cancellationToken);
                await UnitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                Logger.LogError(saveEx, "Failed to save processing history for user {UserId}", batch.UserId);
            }

            throw; // Service Bus will retry via abandon
        }
    }

    private List<SignalEntry> CreateSignalEntries(SignalRoutedBatchEvent batch)
    {
        if (IncludeScheduledWindowStart)
        {
            return batch.Signals.Select(s => SignalEntry.Create(
                userId: s.UserId,
                eventType: s.EventType,
                priority: s.Priority,
                windowType: s.WindowType,
                createdAt: s.CreatedAt,
                targetEntityType: s.TargetEntityType,
                targetEntityId: s.TargetEntityId,
                scheduledWindowStart: s.ScheduledWindowStart)).ToList();
        }

        return batch.Signals.Select(s => SignalEntry.Create(
            userId: s.UserId,
            eventType: s.EventType,
            priority: s.Priority,
            windowType: s.WindowType,
            createdAt: s.CreatedAt,
            targetEntityType: s.TargetEntityType,
            targetEntityId: s.TargetEntityId)).ToList();
    }
}
