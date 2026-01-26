using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

public sealed class Timezone : ValueObject
{
    public string IanaId { get; }

    private Timezone(string ianaId)
    {
        IanaId = ianaId;
    }

    public static Timezone Create(string ianaId)
    {
        if (string.IsNullOrWhiteSpace(ianaId))
            throw new DomainException("Timezone cannot be empty.");

        // Validate against system time zones
        // Note: On Windows, this may need IANA -> Windows mapping via TimeZoneConverter package
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(ianaId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException($"Invalid timezone: {ianaId}");
        }

        return new Timezone(ianaId);
    }

    public TimeZoneInfo GetTimeZoneInfo() => TimeZoneInfo.FindSystemTimeZoneById(IanaId);

    public DateTime ConvertFromUtc(DateTime utcTime) => TimeZoneInfo.ConvertTimeFromUtc(utcTime, GetTimeZoneInfo());

    public DateTime ConvertToUtc(DateTime localTime) => TimeZoneInfo.ConvertTimeToUtc(localTime, GetTimeZoneInfo());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IanaId;
    }

    public override string ToString() => IanaId;

    public static implicit operator string(Timezone timezone) => timezone.IanaId;
}
