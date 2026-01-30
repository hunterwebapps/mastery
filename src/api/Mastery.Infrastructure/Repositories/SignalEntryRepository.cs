using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

/// <summary>
/// Repository for signal entry operations.
/// </summary>
public class SignalEntryRepository(MasteryDbContext _context) : ISignalEntryRepository
{
    public async Task AddRangeAsync(IEnumerable<SignalEntry> signals, CancellationToken ct = default)
    {
        await _context.SignalEntries.AddRangeAsync(signals, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsForWindowAsync(
        string userId,
        string eventType,
        DateOnly windowDate,
        CancellationToken ct = default)
    {
        var startOfDay = windowDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = windowDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await _context.SignalEntries
            .AsNoTracking()
            .AnyAsync(s =>
                s.UserId == userId &&
                s.EventType == eventType &&
                s.ScheduledWindowStart >= startOfDay &&
                s.ScheduledWindowStart <= endOfDay &&
                s.Status != SignalStatus.Expired &&
                s.Status != SignalStatus.Failed,
                ct);
    }
}
