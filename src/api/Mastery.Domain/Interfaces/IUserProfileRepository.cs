using Mastery.Domain.Entities.UserProfile;

namespace Mastery.Domain.Interfaces;

public interface IUserProfileRepository : IRepository<UserProfile>
{
    /// <summary>
    /// Gets a user profile by auth system user ID.
    /// </summary>
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user profile by auth system user ID, including the current season.
    /// </summary>
    Task<UserProfile?> GetByUserIdWithSeasonAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a profile exists for the given auth system user ID.
    /// </summary>
    Task<bool> ExistsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
