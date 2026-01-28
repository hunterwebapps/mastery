using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Recommendations.SignalDetectors;

public sealed class CapacityOverloadDetector : IDiagnosticSignalDetector
{
    private const int MaxTaskCount = 10;
    private const int MaxMinutes = 600;

    public IReadOnlyList<SignalType> SupportedSignals { get; } = [SignalType.CapacityOverload];

    public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(UserStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var signals = new List<DiagnosticSignal>();

        var todayTasks = state.Tasks
            .Where(t => t.ScheduledDate == state.Today)
            .ToList();

        var taskCount = todayTasks.Count;
        var totalMinutes = todayTasks.Sum(t => t.EstMinutes ?? 0);

        if (taskCount > MaxTaskCount || totalMinutes > MaxMinutes)
        {
            var reason = taskCount > MaxTaskCount && totalMinutes > MaxMinutes
                ? $"{taskCount} tasks and {totalMinutes} minutes scheduled"
                : taskCount > MaxTaskCount
                    ? $"{taskCount} tasks scheduled (max {MaxTaskCount})"
                    : $"{totalMinutes} minutes scheduled (max {MaxMinutes})";

            var evidence = SignalEvidence.Create(
                taskCount > MaxTaskCount ? "ScheduledTaskCount" : "ScheduledMinutesToday",
                taskCount > MaxTaskCount ? taskCount : totalMinutes,
                taskCount > MaxTaskCount ? MaxTaskCount : MaxMinutes,
                reason);

            signals.Add(DiagnosticSignal.Create(
                state.UserId,
                SignalType.CapacityOverload,
                "Capacity overload detected",
                $"Today's workload exceeds safe capacity: {reason}. Consider deferring or descoping.",
                80,
                evidence,
                state.Today));
        }

        return Task.FromResult<IReadOnlyList<DiagnosticSignal>>(signals);
    }
}
