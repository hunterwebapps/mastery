using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Recommendations.SignalDetectors;

public sealed class Top1FollowThroughLowDetector : IDiagnosticSignalDetector
{
    private const decimal CompletionThreshold = 0.5m;

    public IReadOnlyList<SignalType> SupportedSignals { get; } = [SignalType.Top1FollowThroughLow];

    public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(UserStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var signals = new List<DiagnosticSignal>();

        var sevenDaysAgo = state.Today.AddDays(-7);
        var recentMorningCheckIns = state.RecentCheckIns
            .Where(c => c.Date >= sevenDaysAgo && c.Type == CheckInType.Morning)
            .ToList();

        var withTop1 = recentMorningCheckIns
            .Where(c => c.Top1EntityId.HasValue)
            .ToList();

        var hasTop1Count = withTop1.Count;

        if (hasTop1Count > 0)
        {
            var completedCount = withTop1.Count(c => c.Top1Completed == true);
            var completedRate = (decimal)completedCount / hasTop1Count;

            if (completedRate < CompletionThreshold)
            {
                var evidence = SignalEvidence.Create(
                    "Top1CompletionRate",
                    completedRate,
                    CompletionThreshold,
                    $"{completedCount} of {hasTop1Count} Top-1 priorities completed in the last 7 days");

                signals.Add(DiagnosticSignal.Create(
                    state.UserId,
                    SignalType.Top1FollowThroughLow,
                    "Low Top-1 follow-through",
                    $"Only {completedCount} of {hasTop1Count} Top-1 priorities were completed in the last 7 days ({completedRate:P0}). The Top-1 should be the most protected commitment of the day.",
                    65,
                    evidence,
                    state.Today));
            }
        }

        return Task.FromResult<IReadOnlyList<DiagnosticSignal>>(signals);
    }
}
