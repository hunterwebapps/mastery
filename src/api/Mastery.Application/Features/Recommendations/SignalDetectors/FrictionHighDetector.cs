using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Recommendations.SignalDetectors;

public sealed class FrictionHighDetector : IDiagnosticSignalDetector
{
    private const int RescheduleThreshold = 2;

    public IReadOnlyList<SignalType> SupportedSignals { get; } = [SignalType.FrictionHigh];

    public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(UserStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var signals = new List<DiagnosticSignal>();

        foreach (var task in state.Tasks)
        {
            if (task.RescheduleCount > RescheduleThreshold)
            {
                var severity = Math.Min(100, task.RescheduleCount * 15);
                var evidence = SignalEvidence.Create(
                    "RescheduleCount",
                    task.RescheduleCount,
                    RescheduleThreshold,
                    $"Task '{task.Title}' has been rescheduled {task.RescheduleCount} times");

                signals.Add(DiagnosticSignal.Create(
                    state.UserId,
                    SignalType.FrictionHigh,
                    $"High friction task: {task.Title}",
                    $"Task '{task.Title}' has been rescheduled {task.RescheduleCount} times, indicating persistent friction. Consider breaking it down or removing blockers.",
                    severity,
                    evidence,
                    state.Today));
            }
        }

        return Task.FromResult<IReadOnlyList<DiagnosticSignal>>(signals);
    }
}
