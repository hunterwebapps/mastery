namespace Mastery.Domain.Entities.UserProfile;

/// <summary>
/// Represents a life role the user identifies with (stored as JSON in UserProfile).
/// Roles define identity areas and help with time allocation.
/// </summary>
public sealed record UserRole
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Optional standardized key for the role (e.g., "parent", "engineer", "partner").
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// User-facing label for the role.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Stable importance rank in life (1 = most important).
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// How central this role is in the current season (1-5 scale).
    /// 5 = primary focus, 1 = maintenance mode.
    /// </summary>
    public int SeasonPriority { get; init; } = 3;

    /// <summary>
    /// Non-negotiable minimum minutes per week for this role.
    /// </summary>
    public int MinWeeklyMinutes { get; init; }

    /// <summary>
    /// Target minutes per week when fully engaged.
    /// </summary>
    public int TargetWeeklyMinutes { get; init; }

    /// <summary>
    /// Context tags for this role (e.g., "deep_work", "social", "admin").
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Whether this role is currently active.
    /// </summary>
    public RoleStatus Status { get; init; } = RoleStatus.Active;
}

public enum RoleStatus
{
    Active,
    Inactive
}
