using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class DiagnosticSignalRepository : BaseRepository<DiagnosticSignal>, IDiagnosticSignalRepository
{
    public DiagnosticSignalRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<DiagnosticSignal>> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.Severity)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiagnosticSignal>> GetByUserIdAndTypeAsync(
        string userId,
        SignalType type,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId && s.Type == type)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
