using DotNetCore.CAP;
using Mastery.Application.Common.Interfaces;
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
    IUserStateAssembler stateAssembler,
    ITieredAssessmentEngine tieredEngine,
    ISignalEntryRepository signalEntryRepository,
    ISignalProcessingHistoryRepository historyRepo,
    IRecommendationRepository recommendationRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<BatchSignalConsumer> logger)
    : BaseSignalConsumer<BatchSignalConsumer>(
        stateAssembler, tieredEngine, signalEntryRepository, historyRepo,
        recommendationRepo, unitOfWork, dateTimeProvider, logger), ICapSubscribe
{
    protected override ProcessingWindowType GetWindowType(SignalRoutedBatchEvent batch)
        => ProcessingWindowType.BatchWindow;

    protected override LogLevel CompletionLogLevel => LogLevel.Debug;

    protected override string GetSignalTypeDescription(SignalRoutedBatchEvent batch)
    {
        var priority = batch.Signals.FirstOrDefault()?.Priority ?? SignalPriority.Standard;
        return priority.ToString().ToLowerInvariant();
    }

    [CapSubscribe("signals-batch")]
    public Task HandleBatchAsync(SignalRoutedBatchEvent batch, CancellationToken cancellationToken)
        => ProcessBatchAsync(batch, cancellationToken);
}
