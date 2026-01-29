using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

/// <summary>
/// Repository for signal queue operations with lease-based concurrency control.
/// Uses UPDLOCK, READPAST hints for atomic batch acquisition without blocking.
/// </summary>
public class SignalQueueRepository : ISignalQueue
{
    private readonly MasteryDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SignalQueueRepository(MasteryDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task EnqueueAsync(SignalEntry signal, CancellationToken ct = default)
    {
        await _context.SignalEntries.AddAsync(signal, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task EnqueueBatchAsync(IEnumerable<SignalEntry> signals, CancellationToken ct = default)
    {
        await _context.SignalEntries.AddRangeAsync(signals, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SignalEntry>> AcquireBatchAsync(
        string workerId,
        SignalPriority maxPriority,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var leaseUntil = now.Add(leaseDuration);

        // Use raw SQL with UPDLOCK, READPAST to atomically select and lock rows
        // This prevents multiple workers from acquiring the same entries
        var entries = await _context.SignalEntries
            .FromSqlRaw(
                """
                SELECT TOP ({0}) *
                FROM SignalEntries WITH (UPDLOCK, READPAST)
                WHERE Status = 'Pending'
                  AND Priority <= {1}
                  AND ExpiresAt > {2}
                  AND (WindowType = 'Immediate'
                       OR ScheduledWindowStart IS NULL
                       OR ScheduledWindowStart <= {2})
                ORDER BY Priority, CreatedAt
                """,
                batchSize,
                (int)maxPriority,
                now)
            .ToListAsync(ct);

        // Update entries to Processing status with lease info
        foreach (var entry in entries)
        {
            entry.TryAcquireLease(workerId, leaseUntil, now);
        }

        if (entries.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return entries;
    }

    public async Task<IReadOnlyList<SignalEntry>> GetPendingForUserWindowAsync(
        string userId,
        ProcessingWindowType windowType,
        CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;

        return await _context.SignalEntries
            .Where(s => s.UserId == userId
                     && s.Status == SignalStatus.Pending
                     && s.WindowType == windowType
                     && s.ExpiresAt > now)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SignalEntry>> GetPendingForUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;

        return await _context.SignalEntries
            .Where(s => s.UserId == userId
                     && s.Status == SignalStatus.Pending
                     && s.ExpiresAt > now)
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task MarkProcessedAsync(
        IEnumerable<long> signalIds,
        AssessmentTier tier,
        CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var idList = signalIds.ToList();

        if (idList.Count == 0)
            return;

        var entries = await _context.SignalEntries
            .Where(s => idList.Contains(s.Id))
            .ToListAsync(ct);

        foreach (var entry in entries)
        {
            entry.MarkProcessed(now, tier);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkSkippedAsync(
        IEnumerable<long> signalIds,
        string reason,
        CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var idList = signalIds.ToList();

        if (idList.Count == 0)
            return;

        var entries = await _context.SignalEntries
            .Where(s => idList.Contains(s.Id))
            .ToListAsync(ct);

        foreach (var entry in entries)
        {
            entry.MarkSkipped(now, reason);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetUsersForWindowByTimezoneAsync(
        ProcessingWindowType windowType,
        DateTime windowStartUtc,
        CancellationToken ct = default)
    {
        // Get distinct user IDs with pending signals for this window type
        // Note: Timezone grouping would require joining with UserProfile
        // For now, return all users in a single "default" timezone group
        var now = _dateTimeProvider.UtcNow;

        var userIds = await _context.SignalEntries
            .Where(s => s.Status == SignalStatus.Pending
                     && s.WindowType == windowType
                     && s.ExpiresAt > now
                     && (s.ScheduledWindowStart == null || s.ScheduledWindowStart <= windowStartUtc))
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);

        // Return in a single timezone group for now
        // TODO: Join with UserProfile to get actual timezone bands
        return new Dictionary<string, IReadOnlyList<string>>
        {
            ["UTC"] = userIds
        };
    }

    public async Task<IReadOnlyList<string>> GetUsersWithUrgentSignalsAsync(CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;

        return await _context.SignalEntries
            .Where(s => s.Status == SignalStatus.Pending
                     && s.Priority == SignalPriority.Urgent
                     && s.ExpiresAt > now)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<int> ExpireOldSignalsAsync(DateTime now, CancellationToken ct = default)
    {
        // Mark pending signals that have exceeded their TTL as expired
        var expiredCount = await _context.SignalEntries
            .Where(s => s.Status == SignalStatus.Pending && s.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.Status, SignalStatus.Expired),
                ct);

        return expiredCount;
    }

    public async Task<int> ReleaseExpiredLeasesAsync(DateTime now, CancellationToken ct = default)
    {
        var expiredEntries = await _context.SignalEntries
            .Where(s => s.Status == SignalStatus.Processing
                     && s.LeasedUntil.HasValue
                     && s.LeasedUntil.Value < now)
            .ToListAsync(ct);

        foreach (var entry in expiredEntries)
        {
            entry.ReleaseLease();
        }

        if (expiredEntries.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return expiredEntries.Count;
    }

    public async Task<SignalEntry?> GetByIdAsync(long signalId, CancellationToken ct = default)
    {
        return await _context.SignalEntries.FindAsync([signalId], ct);
    }

    public async Task<IReadOnlyDictionary<SignalStatus, int>> GetStatusCountsAsync(CancellationToken ct = default)
    {
        var counts = await _context.SignalEntries
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(x => x.Status, x => x.Count);
    }
}
