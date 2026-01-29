using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Resolves user-specific processing window schedules based on timezone and preferences.
/// </summary>
public sealed class UserScheduleResolver : IUserScheduleResolver
{
    private readonly MasteryDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UserScheduleResolver(MasteryDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DateTime> GetNextWindowStartAsync(
        string userId,
        ProcessingWindowType windowType,
        CancellationToken ct = default)
    {
        var boundaries = await GetWindowBoundariesAsync(userId, windowType, ct);
        return boundaries.StartUtc;
    }

    public async Task<WindowBoundaries> GetWindowBoundariesAsync(
        string userId,
        ProcessingWindowType windowType,
        CancellationToken ct = default)
    {
        var profile = await GetUserProfileAsync(userId, ct);
        if (profile == null)
        {
            // Return default boundaries for unknown user
            return GetDefaultWindowBoundaries(windowType, "UTC");
        }

        return GetWindowBoundariesForProfile(profile, windowType);
    }

    public async Task<bool> IsWithinWindowAsync(
        string userId,
        ProcessingWindowType windowType,
        CancellationToken ct = default)
    {
        var boundaries = await GetWindowBoundariesAsync(userId, windowType, ct);
        return boundaries.IsCurrentlyActive;
    }

    public async Task<IReadOnlyList<UserWindowInfo>> GetUsersInWindowRangeAsync(
        ProcessingWindowType windowType,
        DateTime utcStart,
        DateTime utcEnd,
        CancellationToken ct = default)
    {
        // Get all user profiles with their timezones
        var profiles = await _context.UserProfiles
            .AsNoTracking()
            .ToListAsync(ct);

        var results = new List<UserWindowInfo>();

        foreach (var profile in profiles)
        {
            var boundaries = GetWindowBoundariesForProfile(profile, windowType);

            // Check if the window falls within the requested UTC range
            if (boundaries.StartUtc >= utcStart && boundaries.StartUtc <= utcEnd)
            {
                results.Add(new UserWindowInfo(
                    profile.UserId,
                    profile.Timezone.IanaId,
                    boundaries.StartUtc,
                    boundaries.EndUtc));
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<string>> GetDistinctTimezoneBandsAsync(CancellationToken ct = default)
    {
        var timezones = await _context.UserProfiles
            .AsNoTracking()
            .Select(p => p.Timezone.IanaId)
            .Distinct()
            .ToListAsync(ct);

        // Group by UTC offset bands (4-hour increments)
        var bands = timezones
            .Select(tz =>
            {
                try
                {
                    var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                    var offset = tzInfo.GetUtcOffset(_dateTimeProvider.UtcNow);
                    // Round to nearest 4-hour band
                    var bandHours = (int)Math.Round(offset.TotalHours / 4.0) * 4;
                    return $"UTC{(bandHours >= 0 ? "+" : "")}{bandHours}";
                }
                catch
                {
                    return "UTC+0";
                }
            })
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        return bands;
    }

    private async Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken ct)
    {
        return await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    private WindowBoundaries GetWindowBoundariesForProfile(UserProfile profile, ProcessingWindowType windowType)
    {
        var now = _dateTimeProvider.UtcNow;
        var tz = profile.Timezone.GetTimeZoneInfo();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, tz);
        var prefs = profile.Preferences.ProcessingWindows;

        return windowType switch
        {
            ProcessingWindowType.Immediate => new WindowBoundaries(now, now.AddMinutes(5), true),
            ProcessingWindowType.MorningWindow => GetMorningWindowBoundaries(localNow, prefs, tz),
            ProcessingWindowType.EveningWindow => GetEveningWindowBoundaries(localNow, prefs, tz),
            ProcessingWindowType.WeeklyReview => GetWeeklyReviewBoundaries(localNow, prefs, tz),
            ProcessingWindowType.BatchWindow => GetBatchWindowBoundaries(now),
            _ => new WindowBoundaries(now, now.AddHours(1), true)
        };
    }

    private WindowBoundaries GetMorningWindowBoundaries(
        DateTime localNow,
        ProcessingWindowPreferences prefs,
        TimeZoneInfo tz)
    {
        var today = DateOnly.FromDateTime(localNow);
        var windowStart = prefs.MorningWindowStart;
        var windowEnd = prefs.MorningWindowEnd;

        // Calculate local start/end for today
        var localStart = today.ToDateTime(windowStart);
        var localEnd = today.ToDateTime(windowEnd);

        // If we're past today's window, move to tomorrow
        if (localNow > localEnd)
        {
            localStart = localStart.AddDays(1);
            localEnd = localEnd.AddDays(1);
        }

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);
        var isActive = localNow >= localStart && localNow <= localEnd;

        return new WindowBoundaries(utcStart, utcEnd, isActive);
    }

    private WindowBoundaries GetEveningWindowBoundaries(
        DateTime localNow,
        ProcessingWindowPreferences prefs,
        TimeZoneInfo tz)
    {
        var today = DateOnly.FromDateTime(localNow);
        var windowStart = prefs.EveningWindowStart;
        var windowEnd = prefs.EveningWindowEnd;

        // Calculate local start/end for today
        var localStart = today.ToDateTime(windowStart);
        var localEnd = today.ToDateTime(windowEnd);

        // If we're past today's window, move to tomorrow
        if (localNow > localEnd)
        {
            localStart = localStart.AddDays(1);
            localEnd = localEnd.AddDays(1);
        }

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);
        var isActive = localNow >= localStart && localNow <= localEnd;

        return new WindowBoundaries(utcStart, utcEnd, isActive);
    }

    private WindowBoundaries GetWeeklyReviewBoundaries(
        DateTime localNow,
        ProcessingWindowPreferences prefs,
        TimeZoneInfo tz)
    {
        var today = DateOnly.FromDateTime(localNow);
        var currentDayOfWeek = localNow.DayOfWeek;
        var targetDay = prefs.WeeklyReviewDay;

        // Calculate days until next target day
        var daysUntilTarget = ((int)targetDay - (int)currentDayOfWeek + 7) % 7;

        // If today is the target day, check if we're past the window
        if (daysUntilTarget == 0)
        {
            var todayWindowEnd = today.ToDateTime(prefs.WeeklyReviewEnd);
            if (localNow > todayWindowEnd)
            {
                daysUntilTarget = 7; // Next week
            }
        }

        var targetDate = today.AddDays(daysUntilTarget);
        var localStart = targetDate.ToDateTime(prefs.WeeklyReviewStart);
        var localEnd = targetDate.ToDateTime(prefs.WeeklyReviewEnd);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);
        var isActive = localNow >= localStart && localNow <= localEnd;

        return new WindowBoundaries(utcStart, utcEnd, isActive);
    }

    private WindowBoundaries GetBatchWindowBoundaries(DateTime utcNow)
    {
        // Batch windows run every 3-4 hours
        // Return next batch window (aligned to 3-hour intervals)
        var hour = utcNow.Hour;
        var nextBatchHour = ((hour / 3) + 1) * 3;

        DateTime batchStart;
        if (nextBatchHour >= 24)
        {
            batchStart = utcNow.Date.AddDays(1);
        }
        else
        {
            batchStart = utcNow.Date.AddHours(nextBatchHour);
        }

        var batchEnd = batchStart.AddHours(1);
        var isActive = utcNow >= batchStart.AddHours(-3) && utcNow <= batchEnd;

        return new WindowBoundaries(batchStart, batchEnd, isActive);
    }

    private WindowBoundaries GetDefaultWindowBoundaries(ProcessingWindowType windowType, string timezoneId)
    {
        var now = _dateTimeProvider.UtcNow;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, tz);
            var defaultPrefs = new ProcessingWindowPreferences();

            return windowType switch
            {
                ProcessingWindowType.Immediate => new WindowBoundaries(now, now.AddMinutes(5), true),
                ProcessingWindowType.MorningWindow => GetMorningWindowBoundaries(localNow, defaultPrefs, tz),
                ProcessingWindowType.EveningWindow => GetEveningWindowBoundaries(localNow, defaultPrefs, tz),
                ProcessingWindowType.WeeklyReview => GetWeeklyReviewBoundaries(localNow, defaultPrefs, tz),
                ProcessingWindowType.BatchWindow => GetBatchWindowBoundaries(now),
                _ => new WindowBoundaries(now, now.AddHours(1), true)
            };
        }
        catch
        {
            // Fallback to UTC
            return new WindowBoundaries(now, now.AddHours(1), true);
        }
    }
}
