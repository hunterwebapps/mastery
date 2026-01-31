using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer that handles window-aligned (P1) signal batch messages from the signals-window queue.
/// These messages are typically scheduled for delivery at the user's morning/evening windows.
/// </summary>
public sealed class WindowSignalConsumer(
    IOptions<ServiceBusOptions> options,
    IUserStateAssembler stateAssembler,
    ITieredAssessmentEngine tieredEngine,
    ISignalEntryRepository signalEntryRepository,
    ISignalProcessingHistoryRepository historyRepo,
    IRecommendationRepository recommendationRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<WindowSignalConsumer> logger)
    : BaseSignalConsumer<WindowSignalConsumer>(
        options.Value.WindowQueueName,
        stateAssembler, tieredEngine, signalEntryRepository, historyRepo,
        recommendationRepo, unitOfWork, dateTimeProvider, logger)
{
    protected override ProcessingWindowType GetWindowType(SignalRoutedBatchEvent batch)
        => batch.Signals.FirstOrDefault()?.WindowType ?? ProcessingWindowType.MorningWindow;

    protected override bool IncludeScheduledWindowStart => true;

    protected override string GetSignalTypeDescription(SignalRoutedBatchEvent batch)
        => "window";
}
