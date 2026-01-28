using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Habit;

/// <summary>
/// Represents a mode variant of a habit (Full, Maintenance, or Minimum version).
/// Enables graceful degradation during low-capacity periods.
/// </summary>
public sealed class HabitVariant : BaseEntity
{
    /// <summary>
    /// The habit this variant belongs to.
    /// </summary>
    public Guid HabitId { get; private set; }

    /// <summary>
    /// The mode this variant represents.
    /// </summary>
    public HabitMode Mode { get; private set; }

    /// <summary>
    /// User-friendly label for this variant (e.g., "Full workout" or "Quick stretch").
    /// </summary>
    public string Label { get; private set; } = null!;

    /// <summary>
    /// Default value for metric contributions when this mode is used.
    /// </summary>
    public decimal DefaultValue { get; private set; }

    /// <summary>
    /// Estimated time in minutes for this variant.
    /// </summary>
    public int EstimatedMinutes { get; private set; }

    /// <summary>
    /// Energy cost on a 1-5 scale.
    /// </summary>
    public int EnergyCost { get; private set; }

    /// <summary>
    /// Whether this variant counts as a "true" completion for streak purposes.
    /// </summary>
    public bool CountsAsCompletion { get; private set; }

    private HabitVariant() { } // EF Core

    public static HabitVariant Create(
        Guid habitId,
        HabitMode mode,
        string label,
        decimal defaultValue,
        int estimatedMinutes,
        int energyCost,
        bool countsAsCompletion = true)
    {
        if (habitId == Guid.Empty)
            throw new DomainException("HabitId cannot be empty.");

        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Variant label cannot be empty.");

        if (label.Length > 100)
            throw new DomainException("Variant label cannot exceed 100 characters.");

        if (estimatedMinutes < 0)
            throw new DomainException("Estimated minutes cannot be negative.");

        if (energyCost < 1 || energyCost > 5)
            throw new DomainException("Energy cost must be between 1 and 5.");

        return new HabitVariant
        {
            HabitId = habitId,
            Mode = mode,
            Label = label,
            DefaultValue = defaultValue,
            EstimatedMinutes = estimatedMinutes,
            EnergyCost = energyCost,
            CountsAsCompletion = countsAsCompletion
        };
    }

    public void Update(
        string? label = null,
        decimal? defaultValue = null,
        int? estimatedMinutes = null,
        int? energyCost = null,
        bool? countsAsCompletion = null)
    {
        if (label != null)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new DomainException("Variant label cannot be empty.");
            if (label.Length > 100)
                throw new DomainException("Variant label cannot exceed 100 characters.");
            Label = label;
        }

        if (defaultValue.HasValue)
            DefaultValue = defaultValue.Value;

        if (estimatedMinutes.HasValue)
        {
            if (estimatedMinutes.Value < 0)
                throw new DomainException("Estimated minutes cannot be negative.");
            EstimatedMinutes = estimatedMinutes.Value;
        }

        if (energyCost.HasValue)
        {
            if (energyCost.Value < 1 || energyCost.Value > 5)
                throw new DomainException("Energy cost must be between 1 and 5.");
            EnergyCost = energyCost.Value;
        }

        if (countsAsCompletion.HasValue)
            CountsAsCompletion = countsAsCompletion.Value;
    }
}
