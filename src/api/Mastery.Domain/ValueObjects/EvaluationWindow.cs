using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents the time window over which a metric is evaluated.
/// </summary>
public sealed class EvaluationWindow : ValueObject
{
    /// <summary>
    /// The type of evaluation window.
    /// </summary>
    public WindowType Type { get; }

    /// <summary>
    /// Number of days for rolling window (only used when Type is Rolling).
    /// </summary>
    public int? RollingDays { get; }

    /// <summary>
    /// Day of week the window starts (0 = Sunday, 1 = Monday, etc.).
    /// Used for Weekly type to customize week start.
    /// </summary>
    public DayOfWeek? StartDay { get; }

    // Required for EF Core and JSON deserialization
    private EvaluationWindow()
    {
        Type = WindowType.Daily;
    }

    [JsonConstructor]
    public EvaluationWindow(WindowType type, int? rollingDays = null, DayOfWeek? startDay = null)
    {
        Type = type;
        RollingDays = rollingDays;
        StartDay = startDay;
    }

    /// <summary>
    /// Creates a new EvaluationWindow with validation.
    /// </summary>
    public static EvaluationWindow Create(WindowType type, int? rollingDays = null, DayOfWeek? startDay = null)
    {
        if (type == WindowType.Rolling)
        {
            if (!rollingDays.HasValue || rollingDays.Value < 1)
                throw new DomainException("Rolling window requires a positive number of days.");

            if (rollingDays.Value > 365)
                throw new DomainException("Rolling window cannot exceed 365 days.");
        }
        else if (rollingDays.HasValue)
        {
            throw new DomainException("Rolling days should only be specified for Rolling window type.");
        }

        return new EvaluationWindow(type, rollingDays, startDay);
    }

    /// <summary>
    /// Creates a daily evaluation window.
    /// </summary>
    public static EvaluationWindow Daily() => new(WindowType.Daily);

    /// <summary>
    /// Creates a weekly evaluation window (Monday to Sunday by default).
    /// </summary>
    public static EvaluationWindow Weekly(DayOfWeek startDay = DayOfWeek.Monday) => new(WindowType.Weekly, null, startDay);

    /// <summary>
    /// Creates a monthly evaluation window.
    /// </summary>
    public static EvaluationWindow Monthly() => new(WindowType.Monthly);

    /// <summary>
    /// Creates a rolling window of N days.
    /// </summary>
    public static EvaluationWindow Rolling(int days) => Create(WindowType.Rolling, days);

    /// <summary>
    /// Gets the date range for this window relative to a given reference date in the user's timezone.
    /// </summary>
    public (DateOnly Start, DateOnly End) GetDateRange(DateOnly referenceDate)
    {
        return Type switch
        {
            WindowType.Daily => (referenceDate, referenceDate),

            WindowType.Weekly => GetWeekRange(referenceDate),

            WindowType.Monthly => (
                new DateOnly(referenceDate.Year, referenceDate.Month, 1),
                new DateOnly(referenceDate.Year, referenceDate.Month, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month))),

            WindowType.Rolling => (
                referenceDate.AddDays(-(RollingDays!.Value - 1)),
                referenceDate),

            _ => (referenceDate, referenceDate)
        };
    }

    private (DateOnly Start, DateOnly End) GetWeekRange(DateOnly referenceDate)
    {
        var dayOfWeek = (int)referenceDate.DayOfWeek;
        var weekStart = (int)(StartDay ?? DayOfWeek.Monday);

        // Calculate days since the start of the week
        var daysSinceStart = (dayOfWeek - weekStart + 7) % 7;
        var startDate = referenceDate.AddDays(-daysSinceStart);
        var endDate = startDate.AddDays(6);

        return (startDate, endDate);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return RollingDays;
        yield return StartDay;
    }

    public override string ToString() => Type switch
    {
        WindowType.Daily => "Daily",
        WindowType.Weekly => StartDay.HasValue ? $"Weekly ({StartDay})" : "Weekly",
        WindowType.Monthly => "Monthly",
        WindowType.Rolling => $"Last {RollingDays} days",
        _ => Type.ToString()
    };
}
