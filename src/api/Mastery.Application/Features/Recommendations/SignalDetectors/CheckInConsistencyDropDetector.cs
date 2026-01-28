using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Recommendations.SignalDetectors;

public sealed class CheckInConsistencyDropDetector : IDiagnosticSignalDetector
{
    public IReadOnlyList<SignalType> SupportedSignals { get; } = [SignalType.CheckInConsistencyDrop];

    public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(UserStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var signals = new List<DiagnosticSignal>();

        if (state.CheckInStreak == 0)
        {
            var evidence = SignalEvidence.Create(
                "CheckInStreak",
                0m,
                1m,
                "Check-in streak has been broken");

            signals.Add(DiagnosticSignal.Create(
                state.UserId,
                SignalType.CheckInConsistencyDrop,
                "Check-in streak broken",
                "Your check-in streak has dropped to zero. Consistent check-ins are the foundation of the feedback loop.",
                60,
                evidence,
                state.Today));
        }

        var sevenDaysAgo = state.Today.AddDays(-7);
        var recentMorningCompleted = state.RecentCheckIns
            .Count(c => c.Date >= sevenDaysAgo
                        && c.Type == CheckInType.Morning
                        && c.Status == CheckInStatus.Completed);

        if (recentMorningCompleted < 3)
        {
            var evidence = SignalEvidence.Create(
                "MorningCheckInsLast7Days",
                recentMorningCompleted,
                3m,
                $"Only {recentMorningCompleted} completed morning check-ins in the last 7 days");

            signals.Add(DiagnosticSignal.Create(
                state.UserId,
                SignalType.CheckInConsistencyDrop,
                "Low morning check-in frequency",
                $"Only {recentMorningCompleted} morning check-ins completed in the last 7 days. The system needs at least 3 to generate reliable signals.",
                state.CheckInStreak == 0 ? 60 : 40,
                evidence,
                state.Today));
        }

        return Task.FromResult<IReadOnlyList<DiagnosticSignal>>(signals);
    }
}
