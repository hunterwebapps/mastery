using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Messaging.Services;

/// <summary>
/// Background service that emits "MorningWindowStart" and "EveningWindowStart" signals
/// at the appropriate times for each user based on their timezone and preferences.
/// Uses fixed wall-clock scheduling: wakes at each 5-minute boundary (:00, :05, :10, etc.)
/// and processes the exact bucket for that interval. This ensures no gaps or overlaps
/// regardless of processing time.
/// </summary>
public sealed class WindowSignalSchedulerService(
    IServiceScopeFactory _scopeFactory,
    ILogger<WindowSignalSchedulerService> _logger,
    TimeProvider _timeProvider)
    : BackgroundService
{
    private readonly int _bucketMinutes = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Window Signal Scheduler started with {BucketMinutes}-minute fixed buckets",
            _bucketMinutes);

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var nextBucket = GetNextBucketStart(now);
                var delayUntilNextBucket = nextBucket - now;

                if (delayUntilNextBucket > TimeSpan.Zero)
                {
                    _logger.LogDebug(
                        "Waiting until next bucket at {NextBucket:HH:mm:ss} UTC ({Delay:N1}s)",
                        nextBucket, delayUntilNextBucket.TotalSeconds);

                    await Task.Delay(delayUntilNextBucket, stoppingToken);
                }

                var bucketStart = GetCurrentBucketStart(_timeProvider.GetUtcNow().UtcDateTime);
                var bucketEnd = bucketStart.AddMinutes(_bucketMinutes);

                await ScheduleWindowSignalsAsync(bucketStart, bucketEnd, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling window signals");
                // Brief delay before retry to avoid tight loop on persistent errors
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Window Signal Scheduler stopped");
    }

    /// <summary>
    /// Gets the start of the current bucket (rounded down to nearest bucket boundary).
    /// </summary>
    private DateTime GetCurrentBucketStart(DateTime utcNow)
    {
        var totalMinutes = (int)utcNow.TimeOfDay.TotalMinutes;
        var bucketMinutes = (totalMinutes / _bucketMinutes) * _bucketMinutes;
        return utcNow.Date.AddMinutes(bucketMinutes);
    }

    /// <summary>
    /// Gets the start of the next bucket boundary.
    /// </summary>
    private DateTime GetNextBucketStart(DateTime utcNow)
    {
        var currentBucket = GetCurrentBucketStart(utcNow);
        return currentBucket.AddMinutes(_bucketMinutes);
    }

    private async Task ScheduleWindowSignalsAsync(DateTime bucketStart, DateTime bucketEnd, CancellationToken ct)
    {
        _logger.LogDebug(
            "Processing bucket {BucketStart:HH:mm} - {BucketEnd:HH:mm} UTC",
            bucketStart, bucketEnd);

        using var scope = _scopeFactory.CreateScope();
        var scheduleResolver = scope.ServiceProvider.GetRequiredService<IUserScheduleResolver>();
        var signalEntryRepository = scope.ServiceProvider.GetRequiredService<ISignalEntryRepository>();
        var routingService = scope.ServiceProvider.GetRequiredService<SignalRoutingService>();

        // Schedule both morning and evening windows for this exact bucket
        await ScheduleWindowTypeAsync(
            ProcessingWindowType.MorningWindow,
            "MorningWindowStart",
            bucketStart,
            bucketEnd,
            scheduleResolver,
            signalEntryRepository,
            routingService,
            ct);

        await ScheduleWindowTypeAsync(
            ProcessingWindowType.EveningWindow,
            "EveningWindowStart",
            bucketStart,
            bucketEnd,
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
        // Get all users whose window starts in this exact bucket
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
                // Safety check for duplicates (handles restarts/crashes mid-bucket)
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
