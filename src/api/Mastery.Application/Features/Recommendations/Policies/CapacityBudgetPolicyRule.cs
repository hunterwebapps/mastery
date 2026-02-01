using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Application.Features.Recommendations.Policies;

/// <summary>
/// Enforces capacity budget constraints.
/// Warns when recommendations would exceed MaxPlannedMinutesWeekday/Weekend.
/// </summary>
public sealed class CapacityBudgetPolicyRule(
    ILogger<CapacityBudgetPolicyRule> _logger)
    : IPolicyRule
{
    public string Name => "CapacityBudget";
    public int Order => 200;

    public Task<PolicyRuleResult> EvaluateAsync(
        IReadOnlyList<Recommendation> recommendations,
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken ct = default)
    {
        var violations = new List<PolicyViolation>();

        var constraints = state.Profile?.Constraints;
        if (constraints == null)
            return Task.FromResult(PolicyRuleResult.NoViolations);

        var isWeekend = state.Today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var maxMinutes = isWeekend
            ? constraints.MaxPlannedMinutesWeekend
            : constraints.MaxPlannedMinutesWeekday;

        // Calculate current planned minutes from scheduled tasks
        var currentPlannedMinutes = state.Tasks
            .Where(t => t.ScheduledDate == state.Today &&
                        t.Status != TaskStatus.Completed &&
                        t.Status != TaskStatus.Cancelled)
            .Sum(t => t.EstMinutes ?? 30);

        // Calculate minutes from recommendations that schedule tasks
        var additionalMinutes = 0;
        foreach (var rec in recommendations
            .Where(r => r.Type == RecommendationType.NextBestAction ||
                        r.Type == RecommendationType.Top1Suggestion ||
                        r.Type == RecommendationType.ScheduleAdjustmentSuggestion))
        {
            if (string.IsNullOrEmpty(rec.ActionPayload))
                continue;

            try
            {
                var payload = JsonDocument.Parse(rec.ActionPayload);
                if (payload.RootElement.TryGetProperty("EstMinutes", out var estMinutes) ||
                    payload.RootElement.TryGetProperty("estimatedMinutes", out estMinutes))
                {
                    additionalMinutes += estMinutes.GetInt32();
                }
                else if (payload.RootElement.TryGetProperty("TaskId", out var taskId) ||
                         payload.RootElement.TryGetProperty("taskId", out taskId))
                {
                    // Find the task and get its estimated minutes
                    var task = state.Tasks.FirstOrDefault(t => t.Id == taskId.GetGuid());
                    if (task != null)
                        additionalMinutes += task.EstMinutes ?? 30;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed payloads
            }
        }

        var totalPlanned = currentPlannedMinutes + additionalMinutes;

        if (totalPlanned > maxMinutes)
        {
            var overagePercent = (decimal)(totalPlanned - maxMinutes) / maxMinutes * 100;

            _logger.LogWarning(
                "Capacity budget exceeded: {TotalPlanned} min > {MaxMinutes} min ({Overage:F0}% over)",
                totalPlanned, maxMinutes, overagePercent);

            // Add warning violation (don't reject, just warn)
            violations.Add(new PolicyViolation(
                RuleName: Name,
                Description: $"Recommendations would exceed daily capacity by {overagePercent:F0}% ({totalPlanned - maxMinutes} minutes)",
                Severity: PolicyViolationSeverity.Warning,
                AffectedRecommendationId: null));
        }

        return Task.FromResult(new PolicyRuleResult(violations));
    }
}
