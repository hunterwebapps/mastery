using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents a target configuration for a metric within a goal.
/// </summary>
public sealed class Target : ValueObject
{
    /// <summary>
    /// The type of comparison to use (AtLeast, AtMost, Between, Exactly).
    /// </summary>
    public TargetType Type { get; }

    /// <summary>
    /// The primary target value (or min value for Between type).
    /// </summary>
    public decimal Value { get; }

    /// <summary>
    /// The maximum value (only used for Between type).
    /// </summary>
    public decimal? MaxValue { get; }

    // Required for EF Core and JSON deserialization
    private Target()
    {
        Type = TargetType.AtLeast;
        Value = 0;
    }

    [JsonConstructor]
    public Target(TargetType type, decimal value, decimal? maxValue = null)
    {
        Type = type;
        Value = value;
        MaxValue = maxValue;
    }

    /// <summary>
    /// Creates a new Target with validation.
    /// </summary>
    public static Target Create(TargetType type, decimal value, decimal? maxValue = null)
    {
        if (type == TargetType.Between)
        {
            if (!maxValue.HasValue)
                throw new DomainException("Between target type requires a max value.");

            if (maxValue.Value <= value)
                throw new DomainException("Max value must be greater than min value for Between target.");
        }
        else if (maxValue.HasValue)
        {
            throw new DomainException($"Max value should only be specified for Between target type.");
        }

        return new Target(type, value, maxValue);
    }

    /// <summary>
    /// Creates an "at least" target (value >= target).
    /// </summary>
    public static Target AtLeast(decimal value) => new(TargetType.AtLeast, value);

    /// <summary>
    /// Creates an "at most" target (value <= target).
    /// </summary>
    public static Target AtMost(decimal value) => new(TargetType.AtMost, value);

    /// <summary>
    /// Creates a "between" target (min <= value <= max).
    /// </summary>
    public static Target Between(decimal minValue, decimal maxValue) => Create(TargetType.Between, minValue, maxValue);

    /// <summary>
    /// Creates an "exactly" target (value == target).
    /// </summary>
    public static Target Exactly(decimal value) => new(TargetType.Exactly, value);

    /// <summary>
    /// Evaluates whether a given value meets this target.
    /// </summary>
    public bool IsMet(decimal actualValue) => Type switch
    {
        TargetType.AtLeast => actualValue >= Value,
        TargetType.AtMost => actualValue <= Value,
        TargetType.Between => actualValue >= Value && actualValue <= MaxValue!.Value,
        TargetType.Exactly => actualValue == Value,
        _ => false
    };

    /// <summary>
    /// Calculates progress toward this target (0.0 to 1.0+).
    /// </summary>
    public decimal GetProgress(decimal actualValue, decimal baseline = 0) => Type switch
    {
        TargetType.AtLeast when Value != baseline => (actualValue - baseline) / (Value - baseline),
        TargetType.AtMost when Value != baseline => (baseline - actualValue) / (baseline - Value),
        TargetType.Between => IsMet(actualValue) ? 1m : 0m,
        TargetType.Exactly => actualValue == Value ? 1m : 0m,
        _ => 0m
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Value;
        yield return MaxValue;
    }

    public override string ToString() => Type switch
    {
        TargetType.AtLeast => $">= {Value}",
        TargetType.AtMost => $"<= {Value}",
        TargetType.Between => $"{Value} - {MaxValue}",
        TargetType.Exactly => $"= {Value}",
        _ => Value.ToString()
    };
}
