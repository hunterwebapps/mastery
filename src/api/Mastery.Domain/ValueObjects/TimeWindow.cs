using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

public sealed class TimeWindow : ValueObject
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    // Required for EF Core and JSON deserialization
    private TimeWindow()
    {
        Start = default;
        End = default;
    }

    [JsonConstructor]
    public TimeWindow(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    public static TimeWindow Create(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            throw new DomainException("End time must be after start time.");

        return new TimeWindow(start, end);
    }

    public static TimeWindow Create(int startHour, int startMinute, int endHour, int endMinute)
    {
        return Create(new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));
    }

    public bool Contains(TimeOnly time) => time >= Start && time <= End;

    public bool Overlaps(TimeWindow other)
    {
        return Start < other.End && other.Start < End;
    }

    public TimeSpan Duration => End - Start;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:HH:mm}-{End:HH:mm}";
}
