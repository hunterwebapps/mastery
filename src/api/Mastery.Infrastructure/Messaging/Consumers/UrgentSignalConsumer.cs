using DotNetCore.CAP;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Messaging.Consumers;

/// <summary>
/// CAP consumer that handles urgent (P0) signal batch messages from the signals-urgent queue.
/// Processes signals with minimal latency through the tiered assessment pipeline.
/// </summary>
public sealed class UrgentSignalConsumer(
    IUserStateAssembler stateAssembler,
    ITieredAssessmentEngine tieredEngine,
    ISignalEntryRepository signalEntryRepository,
    ISignalProcessingHistoryRepository historyRepo,
    IRecommendationRepository recommendationRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<UrgentSignalConsumer> logger)
    : BaseSignalConsumer<UrgentSignalConsumer>(
        stateAssembler, tieredEngine, signalEntryRepository, historyRepo,
        recommendationRepo, unitOfWork, dateTimeProvider, logger), ICapSubscribe
{
    protected override ProcessingWindowType GetWindowType(SignalRoutedBatchEvent batch)
        => ProcessingWindowType.Immediate;

    protected override string GetSignalTypeDescription(SignalRoutedBatchEvent batch)
        => "urgent";

    [CapSubscribe("signals-urgent")]
    public Task HandleBatchAsync(SignalRoutedBatchEvent batch, CancellationToken cancellationToken)
        => ProcessBatchAsync(batch, cancellationToken);
}
