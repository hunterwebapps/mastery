using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Recommendations.SignalDetectors;

public sealed class HabitAdherenceDropDetector : IDiagnosticSignalDetector
{
    public IReadOnlyList<SignalType> SupportedSignals { get; } = [SignalType.HabitAdherenceDrop];

    public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(UserStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var signals = new List<DiagnosticSignal>();

        foreach (var habit in state.Habits)
        {
            if (habit.Status != HabitStatus.Active)
                continue;

            if (habit.Adherence7Day < 0.6m)
            {
                var severity = (int)((1m - habit.Adherence7Day) * 100);
                var evidence = SignalEvidence.Create(
                    "Adherence7Day",
                    habit.Adherence7Day,
                    0.6m,
                    $"Habit '{habit.Title}' has {habit.Adherence7Day:P0} adherence over the last 7 days");

                signals.Add(DiagnosticSignal.Create(
                    state.UserId,
                    SignalType.HabitAdherenceDrop,
                    $"Low adherence: {habit.Title}",
                    $"Habit '{habit.Title}' adherence has dropped to {habit.Adherence7Day:P0}, below the 60% threshold.",
                    severity,
                    evidence,
                    state.Today));
            }
        }

        return Task.FromResult<IReadOnlyList<DiagnosticSignal>>(signals);
    }
}
