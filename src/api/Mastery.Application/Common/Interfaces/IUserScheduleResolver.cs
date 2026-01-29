using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Resolves user-specific processing window schedules based on timezone and preferences.
/// </summary>
public interface IUserScheduleResolver
{
    /// <summary>
    /// Gets the start time (in UTC) for the user's next processing window of a given type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="windowType">The type of processing window.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The UTC start time of the next window.</returns>
    Task<DateTime> GetNextWindowStartAsync(
        string userId,
        ProcessingWindowType windowType,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the window boundaries (start/end in UTC) for a user's processing window.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="windowType">The type of processing window.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The window boundaries in UTC.</returns>
    Task<WindowBoundaries> GetWindowBoundariesAsync(
        string userId,
        ProcessingWindowType windowType,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if the current time falls within the user's processing window.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="windowType">The type of processing window.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if currently within the window.</returns>
    Task<bool> IsWithinWindowAsync(
        string userId,
        ProcessingWindowType windowType,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all users whose specified window starts within the given UTC time range.
    /// Groups users by timezone band for efficient batch processing.
    /// </summary>
    /// <param name="windowType">The type of processing window.</param>
    /// <param name="utcStart">Start of the UTC time range.</param>
    /// <param name="utcEnd">End of the UTC time range.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of users with their window information.</returns>
    Task<IReadOnlyList<UserWindowInfo>> GetUsersInWindowRangeAsync(
        ProcessingWindowType windowType,
        DateTime utcStart,
        DateTime utcEnd,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all distinct timezone bands currently in use.
    /// </summary>
    Task<IReadOnlyList<string>> GetDistinctTimezoneBandsAsync(CancellationToken ct = default);
}

/// <summary>
/// Information about a user's processing window.
/// </summary>
public sealed record UserWindowInfo(
    string UserId,
    string TimezoneId,
    DateTime WindowStartUtc,
    DateTime WindowEndUtc);

/// <summary>
/// Boundaries of a processing window.
/// </summary>
public sealed record WindowBoundaries(
    DateTime StartUtc,
    DateTime EndUtc,
    bool IsCurrentlyActive);

/// <summary>
/// Default window configuration constants.
/// </summary>
public static class DefaultProcessingWindows
{
    /// <summary>
    /// Morning window starts at 6:00 AM local time by default.
    /// </summary>
    public static readonly TimeOnly MorningStart = new(6, 0);

    /// <summary>
    /// Morning window ends at 9:00 AM local time by default.
    /// </summary>
    public static readonly TimeOnly MorningEnd = new(9, 0);

    /// <summary>
    /// Evening window starts at 8:00 PM local time by default.
    /// </summary>
    public static readonly TimeOnly EveningStart = new(20, 0);

    /// <summary>
    /// Evening window ends at 10:00 PM local time by default.
    /// </summary>
    public static readonly TimeOnly EveningEnd = new(22, 0);

    /// <summary>
    /// Weekly review is on Sunday by default.
    /// </summary>
    public static readonly DayOfWeek WeeklyReviewDay = DayOfWeek.Sunday;

    /// <summary>
    /// Weekly review window starts at 5:00 PM local time by default.
    /// </summary>
    public static readonly TimeOnly WeeklyReviewStart = new(17, 0);

    /// <summary>
    /// Weekly review window ends at 8:00 PM local time by default.
    /// </summary>
    public static readonly TimeOnly WeeklyReviewEnd = new(20, 0);
}
