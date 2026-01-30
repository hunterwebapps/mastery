using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Messaging.Services;

/// <summary>
/// Background service that emits "MorningWindowStart" and "EveningWindowStart" signals
/// at the appropriate times for each user based on their timezone and preferences.
/// Runs every 5 minutes and schedules signals for users whose windows start in the next 5-minute bucket.
/// </summary>
public sealed class WindowSignalSchedulerService(
    IServiceScopeFactory _scopeFactory,
    ILogger<WindowSignalSchedulerService> _logger)
    : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _lookAheadWindow = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Window Signal Scheduler started, checking every {Interval} minutes",
            _checkInterval.TotalMinutes);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScheduleWindowSignalsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling window signals");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Window Signal Scheduler stopped");
    }

    private async Task ScheduleWindowSignalsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var scheduleResolver = scope.ServiceProvider.GetRequiredService<IUserScheduleResolver>();
        var signalEntryRepository = scope.ServiceProvider.GetRequiredService<ISignalEntryRepository>();
        var routingService = scope.ServiceProvider.GetRequiredService<SignalRoutingService>();

        var now = DateTime.UtcNow;
        var windowEnd = now.Add(_lookAheadWindow);

        // Schedule both morning and evening windows
        await ScheduleWindowTypeAsync(
            ProcessingWindowType.MorningWindow,
            "MorningWindowStart",
            now,
            windowEnd,
            scheduleResolver,
            signalEntryRepository,
            routingService,
            ct);

        await ScheduleWindowTypeAsync(
            ProcessingWindowType.EveningWindow,
            "EveningWindowStart",
            now,
            windowEnd,
            scheduleResolver,
            signalEntryRepository,
            routingService,
            ct);
    }

    private async Task ScheduleWindowTypeAsync(
        ProcessingWindowType windowType,
        string eventType,
        DateTime utcStart,
        DateTime utcEnd,
        IUserScheduleResolver scheduleResolver,
        ISignalEntryRepository signalEntryRepository,
        SignalRoutingService routingService,
        CancellationToken ct)
    {
        // Get all users whose window starts in the next 5-minute bucket
        var usersInWindow = await scheduleResolver.GetUsersInWindowRangeAsync(
            windowType,
            utcStart,
            utcEnd,
            ct);

        if (usersInWindow.Count == 0)
        {
            _logger.LogDebug(
                "No users found with {WindowType} starting between {Start:HH:mm} and {End:HH:mm} UTC",
                windowType, utcStart, utcEnd);
            return;
        }

        var scheduledCount = 0;
        var skippedCount = 0;

        foreach (var user in usersInWindow)
        {
            try
            {
                // Check for duplicates (prevent scheduling same signal on restarts)
                var windowDate = DateOnly.FromDateTime(user.WindowStartUtc);
                var alreadyExists = await signalEntryRepository.ExistsForWindowAsync(
                    user.UserId,
                    eventType,
                    windowDate,
                    ct);

                if (alreadyExists)
                {
                    skippedCount++;
                    _logger.LogDebug(
                        "Skipping duplicate {EventType} signal for user {UserId} on {Date}",
                        eventType, user.UserId, windowDate);
                    continue;
                }

                // Create and route the signal
                var classification = new SignalClassification(
                    EventType: eventType,
                    Priority: SignalPriority.WindowAligned,
                    WindowType: windowType,
                    TargetEntityType: null,
                    TargetEntityId: null);

                await routingService.RouteSignalsAsync(
                    [classification],
                    SignalPriority.WindowAligned,
                    user.UserId,
                    correlationId: $"window-scheduler-{eventType}-{windowDate:yyyy-MM-dd}",
                    ct);

                scheduledCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error scheduling {EventType} signal for user {UserId}",
                    eventType, user.UserId);
            }
        }

        if (scheduledCount > 0 || skippedCount > 0)
        {
            _logger.LogInformation(
                "Scheduled {ScheduledCount} {EventType} signals, skipped {SkippedCount} duplicates",
                scheduledCount, eventType, skippedCount);
        }
    }
}
