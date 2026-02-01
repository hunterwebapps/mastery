using Mastery.Domain.Entities.Learning;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Provides the current context for a user based on their state.
/// Used for accurate context capture when recording recommendation outcomes.
/// </summary>
public interface IUserContextProvider
{
    /// <summary>
    /// Gets the current context for a user based on today's check-in and profile data.
    /// Falls back to sensible defaults if no check-in data is available.
    /// </summary>
    Task<ContextKey> GetCurrentContextAsync(string userId, CancellationToken ct = default);
}
