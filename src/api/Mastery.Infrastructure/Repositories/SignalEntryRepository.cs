using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Signal;
using Mastery.Infrastructure.Data;

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
}
