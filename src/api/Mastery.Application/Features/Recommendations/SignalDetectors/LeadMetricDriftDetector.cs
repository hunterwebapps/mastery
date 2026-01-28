using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Recommendations.SignalDetectors;

public sealed class LeadMetricDriftDetector : IDiagnosticSignalDetector
{
    private const decimal DriftThreshold = 0.7m;

    public IReadOnlyList<SignalType> SupportedSignals { get; } = [SignalType.LeadMetricDrift];

    public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(UserStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var signals = new List<DiagnosticSignal>();

        foreach (var goal in state.Goals)
        {
            if (goal.Status != GoalStatus.Active)
                continue;

            foreach (var metric in goal.Metrics)
            {
                if (metric.Kind != MetricKind.Lead)
                    continue;

                if (!metric.CurrentValue.HasValue)
                    continue;

                var threshold = metric.TargetValue * DriftThreshold;

                if (metric.CurrentValue.Value < threshold)
                {
                    var evidence = SignalEvidence.Create(
                        metric.MetricName,
                        metric.CurrentValue.Value,
                        threshold,
                        $"Lead metric '{metric.MetricName}' is at {metric.CurrentValue.Value} vs target {metric.TargetValue} (threshold: {threshold})");

                    signals.Add(DiagnosticSignal.Create(
                        state.UserId,
                        SignalType.LeadMetricDrift,
                        $"Lead metric drifting: {metric.MetricName}",
                        $"Lead metric '{metric.MetricName}' for goal '{goal.Title}' is below 70% of target ({metric.CurrentValue.Value} vs {metric.TargetValue}).",
                        60,
                        evidence,
                        state.Today));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<DiagnosticSignal>>(signals);
    }
}
