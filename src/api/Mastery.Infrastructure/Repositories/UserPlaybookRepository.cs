using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Learning;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public sealed class UserPlaybookRepository(MasteryDbContext _context) : IUserPlaybookRepository
{
    public async Task<UserPlaybook?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _context.UserPlaybooks
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public async Task AddAsync(UserPlaybook playbook, CancellationToken ct = default)
    {
        await _context.UserPlaybooks.AddAsync(playbook, ct);
    }

    public Task UpdateAsync(UserPlaybook playbook, CancellationToken ct = default)
    {
        _context.UserPlaybooks.Update(playbook);
        return Task.CompletedTask;
    }
}

public sealed class InterventionOutcomeRepository(MasteryDbContext _context) : IInterventionOutcomeRepository
{
    public async Task AddAsync(InterventionOutcome outcome, CancellationToken ct = default)
    {
        await _context.InterventionOutcomes.AddAsync(outcome, ct);
    }

    public Task UpdateAsync(InterventionOutcome outcome, CancellationToken ct = default)
    {
        _context.InterventionOutcomes.Update(outcome);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<InterventionOutcome>> GetByUserIdAsync(
        string userId,
        int limit = 100,
        CancellationToken ct = default)
    {
        return await _context.InterventionOutcomes
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.RecordedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InterventionOutcome>> GetByRecommendationIdAsync(
        Guid recommendationId,
        CancellationToken ct = default)
    {
        return await _context.InterventionOutcomes
            .Where(o => o.RecommendationId == recommendationId)
            .ToListAsync(ct);
    }

    public async Task<InterventionOutcome?> GetByRecommendationIdSingleAsync(
        Guid recommendationId,
        CancellationToken ct = default)
    {
        return await _context.InterventionOutcomes
            .FirstOrDefaultAsync(o => o.RecommendationId == recommendationId, ct);
    }
}
