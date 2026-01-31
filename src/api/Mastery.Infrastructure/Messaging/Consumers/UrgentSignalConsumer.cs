using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer that handles urgent (P0) signal batch messages from the signals-urgent queue.
/// Processes signals with minimal latency through the tiered assessment pipeline.
/// </summary>
public sealed class UrgentSignalConsumer(
    IOptions<ServiceBusOptions> options,
    IUserStateAssembler stateAssembler,
    ITieredAssessmentEngine tieredEngine,
    ISignalEntryRepository signalEntryRepository,
    ISignalProcessingHistoryRepository historyRepo,
    IRecommendationRepository recommendationRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<UrgentSignalConsumer> logger)
    : BaseSignalConsumer<UrgentSignalConsumer>(
        options.Value.UrgentQueueName,
        stateAssembler, tieredEngine, signalEntryRepository, historyRepo,
        recommendationRepo, unitOfWork, dateTimeProvider, logger)
{
    protected override ProcessingWindowType GetWindowType(SignalRoutedBatchEvent batch)
        => ProcessingWindowType.Immediate;

    protected override string GetSignalTypeDescription(SignalRoutedBatchEvent batch)
        => "urgent";
}
