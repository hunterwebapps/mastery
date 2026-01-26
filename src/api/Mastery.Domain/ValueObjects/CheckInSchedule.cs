using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

public sealed class CheckInSchedule : ValueObject
{
    public TimeOnly MorningTime { get; }
    public TimeOnly EveningTime { get; }

    // Required for EF Core and JSON deserialization
    private CheckInSchedule()
    {
        MorningTime = default;
        EveningTime = default;
    }

    [JsonConstructor]
    public CheckInSchedule(TimeOnly morningTime, TimeOnly eveningTime)
    {
        MorningTime = morningTime;
        EveningTime = eveningTime;
    }

    public static CheckInSchedule Create(TimeOnly morningTime, TimeOnly eveningTime)
    {
        if (eveningTime <= morningTime)
            throw new DomainException("Evening check-in time must be after morning check-in time.");

        return new CheckInSchedule(morningTime, eveningTime);
    }

    public static CheckInSchedule Create(int morningHour, int morningMinute, int eveningHour, int eveningMinute)
    {
        return Create(new TimeOnly(morningHour, morningMinute), new TimeOnly(eveningHour, eveningMinute));
    }

    public static CheckInSchedule Default => new(new TimeOnly(8, 0), new TimeOnly(21, 0));

    public TimeSpan TimeBetweenCheckIns => EveningTime - MorningTime;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MorningTime;
        yield return EveningTime;
    }

    public override string ToString() => $"Morning: {MorningTime:HH:mm}, Evening: {EveningTime:HH:mm}";
}
