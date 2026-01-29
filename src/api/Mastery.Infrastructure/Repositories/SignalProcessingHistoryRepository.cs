using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

/// <summary>
/// Repository for signal processing history operations.
/// </summary>
public class SignalProcessingHistoryRepository : ISignalProcessingHistoryRepository
{
    private readonly MasteryDbContext _context;

    public SignalProcessingHistoryRepository(MasteryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SignalProcessingHistory history, CancellationToken ct = default)
    {
        await _context.SignalProcessingHistory.AddAsync(history, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<SignalProcessingHistory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.SignalProcessingHistory.FindAsync([id], ct);
    }

    public async Task<SignalProcessingHistory?> GetLastForUserAsync(string userId, CancellationToken ct = default)
    {
        return await _context.SignalProcessingHistory
            .Where(h => h.UserId == userId && h.CompletedAt != null)
            .OrderByDescending(h => h.CompletedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<SignalProcessingHistory>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        return await _context.SignalProcessingHistory
            .OrderByDescending(h => h.StartedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SignalProcessingHistory>> GetForUserInRangeAsync(
        string userId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct = default)
    {
        return await _context.SignalProcessingHistory
            .Where(h => h.UserId == userId
                     && h.StartedAt >= startUtc
                     && h.StartedAt <= endUtc)
            .OrderByDescending(h => h.StartedAt)
            .ToListAsync(ct);
    }

    public async Task<ProcessingStatistics> GetStatisticsAsync(
        ProcessingWindowType? windowType,
        DateTime sinceUtc,
        CancellationToken ct = default)
    {
        var query = _context.SignalProcessingHistory
            .Where(h => h.CompletedAt != null && h.StartedAt >= sinceUtc);

        if (windowType.HasValue)
        {
            query = query.Where(h => h.WindowType == windowType.Value);
        }

        var records = await query.ToListAsync(ct);

        if (records.Count == 0)
        {
            return new ProcessingStatistics(
                TotalCycles: 0,
                TotalSignalsProcessed: 0,
                TotalSignalsSkipped: 0,
                TotalRecommendationsGenerated: 0,
                CyclesWithErrors: 0,
                AverageDurationMs: 0,
                TierDistribution: new Dictionary<AssessmentTier, int>());
        }

        var tierDistribution = records
            .GroupBy(r => r.FinalTier)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ProcessingStatistics(
            TotalCycles: records.Count,
            TotalSignalsProcessed: records.Sum(r => r.SignalsProcessed),
            TotalSignalsSkipped: records.Sum(r => r.SignalsSkipped),
            TotalRecommendationsGenerated: records.Sum(r => r.RecommendationsGenerated),
            CyclesWithErrors: records.Count(r => r.ErrorMessage != null),
            AverageDurationMs: records.Where(r => r.DurationMs.HasValue).Average(r => r.DurationMs!.Value),
            TierDistribution: tierDistribution);
    }

    public async Task UpdateAsync(SignalProcessingHistory history, CancellationToken ct = default)
    {
        _context.SignalProcessingHistory.Update(history);
        await _context.SaveChangesAsync(ct);
    }
}
