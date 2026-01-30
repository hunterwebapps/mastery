using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects experiments that have passed their end date without a conclusion.
/// Triggers when: experiment is Active AND end date has passed.
/// </summary>
public sealed class ExperimentStaleRule : DeterministicRuleBase
{
    private const double OverdueRatioCritical = 1.0;  // 100% overdue relative to run window
    private const double OverdueRatioWarning = 0.5;   // 50% overdue relative to run window
    private const int DefaultRunWindowDays = 7;

    public override string RuleId => "EXPERIMENT_STALE";
    public override string RuleName => "Stale Experiment Detection";
    public override string Description => "Detects experiments past their end date that need conclusion or extension.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        var staleExperiments = state.Experiments
            .Where(e =>
                e.Status == ExperimentStatus.Active &&
                e.EndDate < state.Today)
            .Select(e => new
            {
                Experiment = e,
                DaysOverdue = state.Today.DayNumber - e.EndDate!.Value.DayNumber,
                RunWindowDays = e.MeasurementPlan?.RunWindowDays ?? DefaultRunWindowDays
            })
            .OrderByDescending(x => x.DaysOverdue)
            .ToList();

        if (staleExperiments.Count == 0)
            return Task.FromResult(NotTriggered());

        var mostStale = staleExperiments.First();

        // Calculate overdue ratio relative to experiment's run window
        // A 7-day experiment 7 days overdue (100%) is worse than a 90-day experiment 7 days overdue (8%)
        var overdueRatio = (double)mostStale.DaysOverdue / mostStale.RunWindowDays;

        var severity = overdueRatio switch
        {
            >= OverdueRatioCritical => RuleSeverity.High,
            >= OverdueRatioWarning => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var evidence = new Dictionary<string, object>
        {
            ["StaleExperimentCount"] = staleExperiments.Count,
            ["MostStaleId"] = mostStale.Experiment.Id,
            ["MostStaleTitle"] = mostStale.Experiment.Title,
            ["DaysOverdue"] = mostStale.DaysOverdue,
            ["RunWindowDays"] = mostStale.RunWindowDays,
            ["OverdueRatio"] = Math.Round(overdueRatio, 2),
            ["OriginalEndDate"] = mostStale.Experiment.EndDate!.Value.ToString("yyyy-MM-dd"),
            ["AllStaleExperiments"] = staleExperiments.Select(x => new
            {
                x.Experiment.Id,
                x.Experiment.Title,
                x.DaysOverdue,
                x.RunWindowDays
            }).ToList()
        };

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.ExperimentEditSuggestion,
            Context: RecommendationContext.DriftAlert,
            TargetKind: RecommendationTargetKind.Experiment,
            TargetEntityId: mostStale.Experiment.Id,
            TargetEntityTitle: mostStale.Experiment.Title,
            ActionKind: RecommendationActionKind.ReflectPrompt,
            Title: $"\"{mostStale.Experiment.Title}\" ended {mostStale.DaysOverdue} days ago - time to conclude",
            Rationale: $"This experiment reached its end date but hasn't been concluded. Review the results: Did the change have the expected effect? Should you adopt it permanently, abandon it, or extend the experiment?",
            Score: Math.Min(0.60m + (mostStale.DaysOverdue * 0.025m), 0.90m),
            ActionSummary: "Conclude or extend experiment");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}
