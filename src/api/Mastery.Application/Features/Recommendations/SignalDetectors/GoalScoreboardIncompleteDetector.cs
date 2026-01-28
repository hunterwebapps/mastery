using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Recommendations.SignalDetectors;

public sealed class GoalScoreboardIncompleteDetector : IDiagnosticSignalDetector
{
    public IReadOnlyList<SignalType> SupportedSignals { get; } = [SignalType.GoalScoreboardIncomplete];

    public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(UserStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var signals = new List<DiagnosticSignal>();

        foreach (var goal in state.Goals)
        {
            if (goal.Status != GoalStatus.Active)
                continue;

            var hasLag = goal.Metrics.Any(m => m.Kind == MetricKind.Lag);
            var hasLead = goal.Metrics.Any(m => m.Kind == MetricKind.Lead);
            var hasConstraint = goal.Metrics.Any(m => m.Kind == MetricKind.Constraint);

            var missing = new List<string>();
            if (!hasLag) missing.Add("Lag");
            if (!hasLead) missing.Add("Lead");
            if (!hasConstraint) missing.Add("Constraint");

            if (missing.Count > 0)
            {
                var missingText = string.Join(", ", missing);
                var evidence = SignalEvidence.Create(
                    "MissingMetricKinds",
                    missing.Count,
                    0m,
                    $"Goal '{goal.Title}' is missing: {missingText}");

                signals.Add(DiagnosticSignal.Create(
                    state.UserId,
                    SignalType.GoalScoreboardIncomplete,
                    $"Incomplete scoreboard: {goal.Title}",
                    $"Goal '{goal.Title}' is missing {missingText} metric(s). A complete scoreboard needs lag, lead, and constraint metrics.",
                    50,
                    evidence,
                    state.Today));
            }
        }

        return Task.FromResult<IReadOnlyList<DiagnosticSignal>>(signals);
    }
}
