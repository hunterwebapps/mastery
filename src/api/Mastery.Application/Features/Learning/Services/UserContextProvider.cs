using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Learning;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Application.Features.Learning.Services;

/// <summary>
/// Provides the current context for a user based on their state.
/// Uses today's check-in data and profile constraints.
/// </summary>
public sealed class UserContextProvider(
    ICheckInRepository _checkInRepository,
    IUserProfileRepository _userProfileRepository,
    ITaskRepository _taskRepository,
    IDateTimeProvider _dateTimeProvider,
    ILogger<UserContextProvider> _logger)
    : IUserContextProvider
{
    public async Task<ContextKey> GetCurrentContextAsync(string userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        // Get today's check-ins for energy level
        var todayCheckIns = await _checkInRepository.GetTodayStateAsync(userId, today, ct);
        var morningCheckIn = todayCheckIns.FirstOrDefault(c => c.Type == CheckInType.Morning);
        var eveningCheckIn = todayCheckIns.FirstOrDefault(c => c.Type == CheckInType.Evening);

        // Use most recent energy level available
        var energyLevel = eveningCheckIn?.EnergyLevel
            ?? morningCheckIn?.EnergyLevel
            ?? 3; // Default medium

        // Get user profile for constraints and season
        var profile = await _userProfileRepository.GetByUserIdWithSeasonAsync(userId, ct);
        var seasonIntensity = profile?.CurrentSeason?.Intensity ?? 3;

        // Calculate capacity utilization
        var capacityUtilization = await CalculateCapacityUtilizationAsync(
            userId,
            today,
            today.DayOfWeek,
            profile?.GetWeekdayCapacityMinutes() ?? 480,
            profile?.GetWeekendCapacityMinutes() ?? 180,
            ct);

        _logger.LogDebug(
            "Built context for user {UserId}: energy={Energy}, capacity={Capacity:F2}, day={DayOfWeek}, season={Season}",
            userId, energyLevel, capacityUtilization, today.DayOfWeek, seasonIntensity);

        return ContextKey.FromValues(
            energyLevel,
            capacityUtilization,
            today.DayOfWeek,
            seasonIntensity);
    }

    private async Task<decimal> CalculateCapacityUtilizationAsync(
        string userId,
        DateOnly today,
        DayOfWeek dayOfWeek,
        int maxMinutesWeekday,
        int maxMinutesWeekend,
        CancellationToken ct)
    {
        var maxMinutes = dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? maxMinutesWeekend
            : maxMinutesWeekday;

        if (maxMinutes <= 0)
            return 1.0m; // Full capacity as default

        // Get tasks scheduled for today
        var tasks = await _taskRepository.GetByUserIdAsync(userId, ct);
        var todayTasks = tasks
            .Where(t => t.Scheduling?.ScheduledOn == today &&
                        t.Status != TaskStatus.Completed &&
                        t.Status != TaskStatus.Cancelled)
            .ToList();

        var plannedMinutes = todayTasks.Sum(t => t.EstimatedMinutes);

        return (decimal)plannedMinutes / maxMinutes;
    }
}
