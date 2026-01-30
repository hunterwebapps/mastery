using DotNetCore.CAP;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Messaging.Consumers;

/// <summary>
/// CAP consumer that handles batch (P2/P3) signal batch messages from the signals-batch queue.
/// Processes standard and low priority signals for background processing.
/// </summary>
public sealed class BatchSignalConsumer(
    IUserStateAssembler _stateAssembler,
    ITieredAssessmentEngine _tieredEngine,
    ISignalEntryRepository _signalEntryRepository,
    ISignalProcessingHistoryRepository _historyRepo,
    IRecommendationRepository _recommendationRepo,
    IUnitOfWork _unitOfWork,
    IDateTimeProvider _dateTimeProvider,
    ILogger<BatchSignalConsumer> _logger)
    : ICapSubscribe
{
    [CapSubscribe("signals-batch")]
    public async Task HandleBatchAsync(SignalRoutedBatchEvent batch, CancellationToken cancellationToken)
    {
        var priority = batch.Signals.FirstOrDefault()?.Priority ?? SignalPriority.Standard;

        _logger.LogDebug(
            "Processing batch of {Count} {Priority} signals for user {UserId}",
            batch.Signals.Count, priority, batch.UserId);

        var startedAt = _dateTimeProvider.UtcNow;

        // Create SignalEntry instances for audit purposes
        var signals = batch.Signals.Select(s => SignalEntry.Create(
            userId: s.UserId,
            eventType: s.EventType,
            priority: s.Priority,
            windowType: s.WindowType,
            createdAt: s.CreatedAt,
            targetEntityType: s.TargetEntityType,
            targetEntityId: s.TargetEntityId)).ToList();

        // Start tracking this processing cycle
        var history = SignalProcessingHistory.Start(
            batch.UserId,
            ProcessingWindowType.BatchWindow,
            startedAt,
            signalsReceived: batch.Signals.Count);

        try
        {
            // 1. Assemble user state once for the batch
            var state = await _stateAssembler.AssembleAsync(batch.UserId, cancellationToken);

            // 2. Run tiered assessment
            var outcome = await _tieredEngine.AssessAsync(state, signals, cancellationToken);

            // 3. Record tier results
            history.RecordTier0Results(outcome.Statistics.Tier0RulesTriggered);
            if (outcome.Tier1Result != null)
            {
                history.RecordTier1Results(
                    outcome.Tier1Result.CombinedScore,
                    System.Text.Json.JsonSerializer.Serialize(outcome.Tier1Result.DeltaSummary));
            }
            if (outcome.Tier2Executed)
            {
                history.RecordTier2Executed();
            }

            // 4. Persist recommendations
            var recommendationIds = new List<Guid>();
            foreach (var recommendation in outcome.GeneratedRecommendations)
            {
                await _recommendationRepo.AddAsync(recommendation, cancellationToken);
                recommendationIds.Add(recommendation.Id);
            }
            history.RecordRecommendations(outcome.GeneratedRecommendations.Count, recommendationIds);

            // 5. Mark signals as processed and persist for audit
            var now = _dateTimeProvider.UtcNow;
            foreach (var signal in signals)
            {
                signal.MarkProcessed(now, outcome.FinalTier);
            }
            await _signalEntryRepository.AddRangeAsync(signals, cancellationToken);

            // 6. Complete history and save
            history.RecordOutcome(signals.Count, 0);
            history.Complete(_dateTimeProvider.UtcNow);
            await _historyRepo.AddAsync(history, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Processed batch of {Count} {Priority} signals for user {UserId}, generated {RecCount} recommendations via {Tier}",
                batch.Signals.Count, priority, batch.UserId, outcome.GeneratedRecommendations.Count, outcome.FinalTier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch signal batch for user {UserId}", batch.UserId);

            history.RecordError(ex.Message);
            history.Complete(_dateTimeProvider.UtcNow);

            try
            {
                await _historyRepo.AddAsync(history, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save processing history for user {UserId}", batch.UserId);
            }

            throw; // CAP will retry
        }
    }
}
