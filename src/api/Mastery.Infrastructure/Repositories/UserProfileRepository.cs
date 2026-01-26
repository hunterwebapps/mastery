using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class UserProfileRepository : BaseRepository<UserProfile>, IUserProfileRepository
{
    public UserProfileRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<UserProfile?> GetByUserIdWithSeasonAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.CurrentSeason)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.UserId == userId, cancellationToken);
    }
}
