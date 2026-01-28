using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

public interface IDiagnosticSignalDetector
{
    IReadOnlyList<SignalType> SupportedSignals { get; }

    Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(
        UserStateSnapshot state,
        CancellationToken cancellationToken = default);
}
