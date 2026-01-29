using Mastery.Application.Common.Interfaces;
using Mastery.Infrastructure.Data;
using Mastery.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

/// <summary>
/// Repository for outbox entry management with lease-based concurrency control.
/// Uses UPDLOCK, READPAST hints for atomic batch acquisition without blocking.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly MasteryDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OutboxRepository(MasteryDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<OutboxEntry>> AcquireBatchAsync(
        string leaseHolder,
        DateTime leaseUntil,
        int batchSize,
        int maxRetries,
        CancellationToken ct)
    {
        var now = _dateTimeProvider.UtcNow;

        // Use raw SQL with UPDLOCK, READPAST to atomically select and lock rows
        // This prevents multiple workers from acquiring the same entries
        var entries = await _context.OutboxEntries
            .FromSqlRaw(
                """
                SELECT TOP ({0}) *
                FROM OutboxEntries WITH (UPDLOCK, READPAST)
                WHERE Status = 'Pending'
                  AND RetryCount < {1}
                ORDER BY CreatedAt
                """,
                batchSize,
                maxRetries)
            .ToListAsync(ct);

        // Update entries to Processing status with lease info
        foreach (var entry in entries)
        {
            entry.TryAcquireLease(leaseHolder, leaseUntil, now);
        }

        if (entries.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return entries;
    }

    public async Task ReleaseExpiredLeasesAsync(CancellationToken ct)
    {
        var now = _dateTimeProvider.UtcNow;

        // Find entries with expired leases
        var expiredEntries = await _context.OutboxEntries
            .Where(e => e.Status == OutboxEntryStatus.Processing
                     && e.LeasedUntil.HasValue
                     && e.LeasedUntil.Value < now)
            .ToListAsync(ct);

        foreach (var entry in expiredEntries)
        {
            entry.ReleaseLease();
        }

        if (expiredEntries.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateBatchAsync(IEnumerable<OutboxEntry> entries, CancellationToken ct)
    {
        // Entries are already tracked by the context, just save
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> ArchiveProcessedEntriesAsync(DateTime olderThan, int batchSize, CancellationToken ct)
    {
        // Delete processed entries older than the threshold
        // Using ExecuteDelete for efficient bulk deletion
        var deleted = await _context.OutboxEntries
            .Where(e => e.Status == OutboxEntryStatus.Processed
                     && e.ProcessedAt.HasValue
                     && e.ProcessedAt.Value < olderThan)
            .Take(batchSize)
            .ExecuteDeleteAsync(ct);

        return deleted;
    }
}
