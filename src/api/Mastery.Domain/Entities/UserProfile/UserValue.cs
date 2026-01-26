namespace Mastery.Domain.Entities.UserProfile;

/// <summary>
/// Represents a user's core value (stored as JSON in UserProfile).
/// Values guide prioritization and decision-making in the system.
/// </summary>
public sealed record UserValue
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Optional standardized key for the value (e.g., "family", "health", "growth").
    /// Useful for system-level analysis and suggestions.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// User-facing label for the value.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Priority rank (1 = highest priority).
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// Optional numeric weight for weighted scoring (0.0 to 1.0).
    /// If not set, can be derived from rank.
    /// </summary>
    public decimal? Weight { get; init; }

    /// <summary>
    /// User's personal notes about what this value means to them.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// How this value was added (e.g., "onboarding", "reflection", "suggested").
    /// </summary>
    public string? Source { get; init; }
}
