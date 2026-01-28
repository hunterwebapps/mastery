using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents the unit of measurement for a metric.
/// </summary>
public sealed class MetricUnit : ValueObject
{
    /// <summary>
    /// The type/category of unit.
    /// </summary>
    public string UnitType { get; }

    /// <summary>
    /// The display label for the unit (e.g., "min", "hours", "lbs", "$").
    /// </summary>
    public string DisplayLabel { get; }

    /// <summary>
    /// Optional plural form of the display label.
    /// </summary>
    public string? PluralLabel { get; }

    // Required for EF Core and JSON deserialization
    private MetricUnit()
    {
        UnitType = string.Empty;
        DisplayLabel = string.Empty;
    }

    [JsonConstructor]
    public MetricUnit(string unitType, string displayLabel, string? pluralLabel = null)
    {
        UnitType = unitType;
        DisplayLabel = displayLabel;
        PluralLabel = pluralLabel;
    }

    /// <summary>
    /// Creates a new MetricUnit with validation.
    /// </summary>
    public static MetricUnit Create(string unitType, string displayLabel, string? pluralLabel = null)
    {
        if (string.IsNullOrWhiteSpace(unitType))
            throw new DomainException("Unit type cannot be empty.");

        if (string.IsNullOrWhiteSpace(displayLabel))
            throw new DomainException("Display label cannot be empty.");

        return new MetricUnit(unitType, displayLabel, pluralLabel);
    }

    // Common pre-defined units
    public static MetricUnit Minutes => new("duration", "min", "mins");
    public static MetricUnit Hours => new("duration", "hour", "hours");
    public static MetricUnit Count => new("count", "", "");
    public static MetricUnit Sessions => new("count", "session", "sessions");
    public static MetricUnit Percentage => new("percentage", "%", "%");
    public static MetricUnit Dollars => new("currency", "$", "$");
    public static MetricUnit Pounds => new("weight", "lb", "lbs");
    public static MetricUnit Kilograms => new("weight", "kg", "kg");
    public static MetricUnit Rating => new("rating", "/5", "/5");
    public static MetricUnit Boolean => new("boolean", "", "");
    public static MetricUnit Steps => new("count", "step", "steps");
    public static MetricUnit Calories => new("energy", "cal", "cals");
    public static MetricUnit None => new("none", "", "");

    /// <summary>
    /// Formats a value with this unit.
    /// </summary>
    public string Format(decimal value)
    {
        var label = value == 1 ? DisplayLabel : (PluralLabel ?? DisplayLabel);

        if (UnitType == "currency")
            return $"{DisplayLabel}{value:N2}";

        if (UnitType == "percentage")
            return $"{value:N1}{DisplayLabel}";

        if (string.IsNullOrEmpty(label))
            return value.ToString("N1");

        return $"{value:N1} {label}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UnitType;
        yield return DisplayLabel;
        yield return PluralLabel;
    }

    public override string ToString() => DisplayLabel;
}
