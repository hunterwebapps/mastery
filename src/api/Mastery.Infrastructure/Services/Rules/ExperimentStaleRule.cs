using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects experiments that have passed their end date without a conclusion.
/// Triggers when: experiment is Running AND end date has passed.
/// </summary>
public sealed class ExperimentStaleRule : DeterministicRuleBase
{
    private const int OverdueDaysCritical = 7;
    private const int OverdueDaysWarning = 3;

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
                DaysOverdue = state.Today.DayNumber - e.EndDate!.Value.DayNumber
            })
            .OrderByDescending(x => x.DaysOverdue)
            .ToList();

        if (staleExperiments.Count == 0)
            return Task.FromResult(NotTriggered());

        var mostStale = staleExperiments.First();

        var severity = mostStale.DaysOverdue switch
        {
            >= OverdueDaysCritical => RuleSeverity.High,
            >= OverdueDaysWarning => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var evidence = new Dictionary<string, object>
        {
            ["StaleExperimentCount"] = staleExperiments.Count,
            ["MostStaleId"] = mostStale.Experiment.Id,
            ["MostStaleTitle"] = mostStale.Experiment.Title,
            ["DaysOverdue"] = mostStale.DaysOverdue,
            ["OriginalEndDate"] = mostStale.Experiment.EndDate!.Value.ToString("yyyy-MM-dd"),
            ["AllStaleExperiments"] = staleExperiments.Select(x => new
            {
                x.Experiment.Id,
                x.Experiment.Title,
                x.DaysOverdue
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
            Score: 0.75m,
            ActionSummary: "Conclude or extend experiment");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}
