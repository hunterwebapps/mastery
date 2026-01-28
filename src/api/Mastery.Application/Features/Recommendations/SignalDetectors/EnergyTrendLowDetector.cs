using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Recommendations.SignalDetectors;

public sealed class EnergyTrendLowDetector : IDiagnosticSignalDetector
{
    private const decimal LowEnergyThreshold = 2m;
    private const int MinDataPoints = 3;

    public IReadOnlyList<SignalType> SupportedSignals { get; } = [SignalType.EnergyTrendLow];

    public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(UserStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var signals = new List<DiagnosticSignal>();

        var fiveDaysAgo = state.Today.AddDays(-5);
        var morningEnergies = state.RecentCheckIns
            .Where(c => c.Date >= fiveDaysAgo
                        && c.Type == CheckInType.Morning
                        && c.EnergyLevel.HasValue)
            .Select(c => c.EnergyLevel!.Value)
            .ToList();

        if (morningEnergies.Count >= MinDataPoints)
        {
            var average = (decimal)morningEnergies.Average(e => e);

            if (average <= LowEnergyThreshold)
            {
                var evidence = SignalEvidence.Create(
                    "AverageMorningEnergy",
                    average,
                    LowEnergyThreshold,
                    $"Average morning energy of {average:F1} over {morningEnergies.Count} days");

                signals.Add(DiagnosticSignal.Create(
                    state.UserId,
                    SignalType.EnergyTrendLow,
                    "Sustained low energy",
                    $"Average morning energy has been {average:F1}/5 over the last {morningEnergies.Count} days. This may indicate recovery debt or external stressors.",
                    70,
                    evidence,
                    state.Today));
            }
        }

        return Task.FromResult<IReadOnlyList<DiagnosticSignal>>(signals);
    }
}
