namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the type of daily check-in.
/// </summary>
public enum CheckInType
{
    /// <summary>
    /// Morning check-in: energy rating, Top 1 selection, day mode.
    /// </summary>
    Morning,

    /// <summary>
    /// Evening check-in: completion sweep, blocker, reflection.
    /// </summary>
    Evening
}
