using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Interfaces;

public interface IDiagnosticSignalRepository : IRepository<DiagnosticSignal>
{
    Task<IReadOnlyList<DiagnosticSignal>> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiagnosticSignal>> GetByUserIdAndTypeAsync(
        string userId,
        SignalType type,
        CancellationToken cancellationToken = default);
}
