using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents the scheduling pattern for a habit.
/// </summary>
public sealed class HabitSchedule : ValueObject
{
    /// <summary>
    /// The type of schedule.
    /// </summary>
    public ScheduleType Type { get; }

    /// <summary>
    /// Days of the week when the habit is due (for DaysOfWeek type).
    /// </summary>
    public DayOfWeek[]? DaysOfWeek { get; }

    /// <summary>
    /// Optional preferred times for the habit.
    /// </summary>
    public TimeOnly[]? PreferredTimes { get; }

    /// <summary>
    /// Number of times per week (for WeeklyFrequency type).
    /// </summary>
    public int? FrequencyPerWeek { get; }

    /// <summary>
    /// Interval in days (for Interval type, e.g., every 3 days).
    /// </summary>
    public int? IntervalDays { get; }

    /// <summary>
    /// When the schedule starts.
    /// </summary>
    public DateOnly StartDate { get; }

    /// <summary>
    /// Optional end date for the schedule.
    /// </summary>
    public DateOnly? EndDate { get; }

    // Required for EF Core and JSON deserialization
    private HabitSchedule()
    {
        Type = ScheduleType.Daily;
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    [JsonConstructor]
    public HabitSchedule(
        ScheduleType type,
        DayOfWeek[]? daysOfWeek,
        TimeOnly[]? preferredTimes,
        int? frequencyPerWeek,
        int? intervalDays,
        DateOnly startDate,
        DateOnly? endDate)
    {
        Type = type;
        DaysOfWeek = daysOfWeek;
        PreferredTimes = preferredTimes;
        FrequencyPerWeek = frequencyPerWeek;
        IntervalDays = intervalDays;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// Creates a daily schedule.
    /// </summary>
    public static HabitSchedule Daily(DateOnly? startDate = null)
    {
        return new HabitSchedule(
            ScheduleType.Daily,
            null,
            null,
            null,
            null,
            startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            null);
    }

    /// <summary>
    /// Creates a schedule for specific days of the week.
    /// </summary>
    public static HabitSchedule OnDays(DayOfWeek[] days, DateOnly? startDate = null)
    {
        if (days is null || days.Length == 0)
            throw new DomainException("At least one day must be specified for DaysOfWeek schedule.");

        if (days.Length > 7)
            throw new DomainException("Cannot specify more than 7 days.");

        if (days.Distinct().Count() != days.Length)
            throw new DomainException("Duplicate days are not allowed.");

        return new HabitSchedule(
            ScheduleType.DaysOfWeek,
            days.OrderBy(d => d).ToArray(),
            null,
            null,
            null,
            startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            null);
    }

    /// <summary>
    /// Creates a weekly frequency schedule (X times per week, flexible days).
    /// </summary>
    public static HabitSchedule TimesPerWeek(int count, DateOnly? startDate = null)
    {
        if (count < 1 || count > 7)
            throw new DomainException("Frequency per week must be between 1 and 7.");

        return new HabitSchedule(
            ScheduleType.WeeklyFrequency,
            null,
            null,
            count,
            null,
            startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            null);
    }

    /// <summary>
    /// Creates an interval schedule (every N days).
    /// </summary>
    public static HabitSchedule EveryNDays(int interval, DateOnly? startDate = null)
    {
        if (interval < 1)
            throw new DomainException("Interval must be at least 1 day.");

        if (interval > 90)
            throw new DomainException("Interval cannot exceed 90 days.");

        return new HabitSchedule(
            ScheduleType.Interval,
            null,
            null,
            null,
            interval,
            startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            null);
    }

    /// <summary>
    /// Creates a schedule with validation.
    /// </summary>
    public static HabitSchedule Create(
        ScheduleType type,
        DayOfWeek[]? daysOfWeek = null,
        TimeOnly[]? preferredTimes = null,
        int? frequencyPerWeek = null,
        int? intervalDays = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var effectiveStartDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (endDate.HasValue && endDate.Value < effectiveStartDate)
            throw new DomainException("End date cannot be before start date.");

        return type switch
        {
            ScheduleType.Daily => new HabitSchedule(
                ScheduleType.Daily, null, preferredTimes, null, null, effectiveStartDate, endDate),

            ScheduleType.DaysOfWeek => OnDays(daysOfWeek ?? [], effectiveStartDate)
                .WithPreferredTimes(preferredTimes)
                .WithEndDate(endDate),

            ScheduleType.WeeklyFrequency => TimesPerWeek(frequencyPerWeek ?? 1, effectiveStartDate)
                .WithPreferredTimes(preferredTimes)
                .WithEndDate(endDate),

            ScheduleType.Interval => EveryNDays(intervalDays ?? 1, effectiveStartDate)
                .WithPreferredTimes(preferredTimes)
                .WithEndDate(endDate),

            _ => throw new DomainException($"Unknown schedule type: {type}")
        };
    }

    /// <summary>
    /// Checks if the habit is due on the specified date.
    /// </summary>
    public bool IsDueOn(DateOnly date)
    {
        // Check date bounds
        if (date < StartDate)
            return false;

        if (EndDate.HasValue && date > EndDate.Value)
            return false;

        return Type switch
        {
            ScheduleType.Daily => true,

            ScheduleType.DaysOfWeek => DaysOfWeek?.Contains(date.DayOfWeek) ?? false,

            ScheduleType.WeeklyFrequency =>
                // For WeeklyFrequency, we cannot strictly determine if a specific day is "due"
                // The user can complete on any day. This returns true for tracking eligibility.
                true,

            ScheduleType.Interval =>
                (date.DayNumber - StartDate.DayNumber) % (IntervalDays ?? 1) == 0,

            _ => false
        };
    }

    /// <summary>
    /// Gets the next due date starting from (and including) the given date.
    /// </summary>
    public DateOnly GetNextDueDate(DateOnly fromDate)
    {
        var checkDate = fromDate < StartDate ? StartDate : fromDate;

        // For end-bounded schedules
        if (EndDate.HasValue && checkDate > EndDate.Value)
            return DateOnly.MaxValue;

        switch (Type)
        {
            case ScheduleType.Daily:
                return checkDate;

            case ScheduleType.DaysOfWeek:
                for (int i = 0; i < 7; i++)
                {
                    var candidate = checkDate.AddDays(i);
                    if (EndDate.HasValue && candidate > EndDate.Value)
                        return DateOnly.MaxValue;
                    if (DaysOfWeek?.Contains(candidate.DayOfWeek) ?? false)
                        return candidate;
                }
                return DateOnly.MaxValue;

            case ScheduleType.WeeklyFrequency:
                // Any day works for weekly frequency
                return checkDate;

            case ScheduleType.Interval:
                var daysSinceStart = checkDate.DayNumber - StartDate.DayNumber;
                var interval = IntervalDays ?? 1;
                var remainder = daysSinceStart % interval;
                if (remainder == 0)
                    return checkDate;
                var nextDue = checkDate.AddDays(interval - remainder);
                if (EndDate.HasValue && nextDue > EndDate.Value)
                    return DateOnly.MaxValue;
                return nextDue;

            default:
                return checkDate;
        }
    }

    /// <summary>
    /// Gets the expected number of occurrences in a date range.
    /// </summary>
    public int GetExpectedCountInRange(DateOnly start, DateOnly end)
    {
        var effectiveStart = start < StartDate ? StartDate : start;
        var effectiveEnd = EndDate.HasValue && end > EndDate.Value ? EndDate.Value : end;

        if (effectiveEnd < effectiveStart)
            return 0;

        var totalDays = effectiveEnd.DayNumber - effectiveStart.DayNumber + 1;

        return Type switch
        {
            ScheduleType.Daily => totalDays,

            ScheduleType.DaysOfWeek => CountDaysOfWeekInRange(effectiveStart, effectiveEnd),

            ScheduleType.WeeklyFrequency =>
                // For a range, estimate based on weeks
                (int)Math.Ceiling((double)totalDays / 7) * (FrequencyPerWeek ?? 1),

            ScheduleType.Interval =>
                (totalDays + (IntervalDays ?? 1) - 1) / (IntervalDays ?? 1),

            _ => 0
        };
    }

    private int CountDaysOfWeekInRange(DateOnly start, DateOnly end)
    {
        if (DaysOfWeek is null || DaysOfWeek.Length == 0)
            return 0;

        var count = 0;
        var current = start;
        while (current <= end)
        {
            if (DaysOfWeek.Contains(current.DayOfWeek))
                count++;
            current = current.AddDays(1);
        }
        return count;
    }

    /// <summary>
    /// Returns a new schedule with preferred times set.
    /// </summary>
    public HabitSchedule WithPreferredTimes(TimeOnly[]? times)
    {
        return new HabitSchedule(Type, DaysOfWeek, times, FrequencyPerWeek, IntervalDays, StartDate, EndDate);
    }

    /// <summary>
    /// Returns a new schedule with an end date set.
    /// </summary>
    public HabitSchedule WithEndDate(DateOnly? endDate)
    {
        if (endDate.HasValue && endDate.Value < StartDate)
            throw new DomainException("End date cannot be before start date.");

        return new HabitSchedule(Type, DaysOfWeek, PreferredTimes, FrequencyPerWeek, IntervalDays, StartDate, endDate);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return DaysOfWeek is null ? null : string.Join(",", DaysOfWeek.Select(d => (int)d));
        yield return PreferredTimes is null ? null : string.Join(",", PreferredTimes);
        yield return FrequencyPerWeek;
        yield return IntervalDays;
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() => Type switch
    {
        ScheduleType.Daily => "Daily",
        ScheduleType.DaysOfWeek => $"On {string.Join(", ", DaysOfWeek?.Select(d => d.ToString()[..3]) ?? [])}",
        ScheduleType.WeeklyFrequency => $"{FrequencyPerWeek}x per week",
        ScheduleType.Interval => $"Every {IntervalDays} days",
        _ => Type.ToString()
    };
}
