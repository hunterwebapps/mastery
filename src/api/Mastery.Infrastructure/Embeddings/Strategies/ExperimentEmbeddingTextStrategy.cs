using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for Experiment entities.
/// Context depth: Self + Goal titles.
/// </summary>
public sealed class ExperimentEmbeddingTextStrategy(IGoalRepository _goalRepository) : IEmbeddingTextStrategy<Experiment>
{
    public async Task<string?> CompileTextAsync(Experiment entity, CancellationToken ct)
    {
        // Don't embed archived experiments
        if (entity.Status == ExperimentStatus.Archived)
        {
            return null;
        }

        var sb = new StringBuilder();

        // Build leading summary: "{Title}: Testing hypothesis that {Change} will lead to {ExpectedOutcome}"
        var summaryText = $"{entity.Title}: Testing hypothesis that {entity.Hypothesis.Change} will lead to {entity.Hypothesis.ExpectedOutcome}";
        EmbeddingFormatHelper.AppendSummary(sb, "EXPERIMENT", summaryText);

        // Basic info
        sb.AppendLine($"Title: {entity.Title}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Description", entity.Description);

        sb.AppendLine($"Category: {EmbeddingFormatHelper.FormatEnum(entity.Category)}");
        sb.AppendLine($"Status: {FormatExperimentStatus(entity.Status)}");
        sb.AppendLine($"Created from: {EmbeddingFormatHelper.FormatEnum(entity.CreatedFrom)}");

        // Hypothesis details
        sb.AppendLine();
        sb.AppendLine("Hypothesis:");
        sb.AppendLine($"  If I: {entity.Hypothesis.Change}");
        sb.AppendLine($"  Then: {entity.Hypothesis.ExpectedOutcome}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "  Rationale", entity.Hypothesis.Rationale);

        // Measurement plan
        sb.AppendLine();
        sb.AppendLine("Measurement Plan:");
        sb.AppendLine($"  Run window: {entity.MeasurementPlan.RunWindowDays} days");
        sb.AppendLine($"  Baseline window: {entity.MeasurementPlan.BaselineWindowDays} days");

        // Date info with human-readable formatting
        if (entity.StartDate.HasValue)
        {
            sb.AppendLine($"Started: {EmbeddingFormatHelper.FormatDate(entity.StartDate.Value)}");
        }

        if (entity.EndDatePlanned.HasValue)
        {
            sb.AppendLine($"Planned end: {EmbeddingFormatHelper.FormatDate(entity.EndDatePlanned.Value)}");
        }

        // Include linked goals
        if (entity.LinkedGoalIds.Count > 0)
        {
            var goalTitles = new List<string>();
            foreach (var goalId in entity.LinkedGoalIds)
            {
                var goal = await _goalRepository.GetByIdAsync(goalId, ct);
                if (goal != null)
                {
                    goalTitles.Add(goal.Title);
                }
            }

            if (goalTitles.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Related goals: {string.Join(", ", goalTitles)}");
            }
        }

        // Notes
        if (entity.Notes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Notes: {string.Join("; ", entity.Notes)}");
        }

        // Include result if completed - critical for RAG learning
        if (entity.Result != null)
        {
            sb.AppendLine();
            sb.AppendLine("Result:");
            sb.AppendLine($"  Outcome: {EmbeddingFormatHelper.FormatEnum(entity.Result.OutcomeClassification)}");
            sb.AppendLine($"  Effectiveness: {FormatEffectiveness(entity.Result.OutcomeClassification)}");
            EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "  Key learning", entity.Result.NarrativeSummary);

            // Add quantitative outcomes if available
            if (entity.Result.BaselineValue.HasValue && entity.Result.RunValue.HasValue)
            {
                var change = entity.Result.RunValue.Value - entity.Result.BaselineValue.Value;
                var direction = change > 0 ? "increased" : change < 0 ? "decreased" : "unchanged";
                sb.AppendLine($"  Metric change: {direction} from {entity.Result.BaselineValue:F1} to {entity.Result.RunValue:F1}");
            }
        }

        // Add status-based outcome keywords for abandoned experiments
        if (entity.Status == ExperimentStatus.Abandoned)
        {
            sb.AppendLine();
            sb.AppendLine("Outcome: Abandoned (user discontinued experiment before completion)");
        }

        // Domain keywords for semantic search - include outcome keywords
        var outcomeKeywords = entity.Status switch
        {
            ExperimentStatus.Completed when entity.Result?.OutcomeClassification == ExperimentOutcome.Positive =>
                "successful positive worked effective improved",
            ExperimentStatus.Completed when entity.Result?.OutcomeClassification == ExperimentOutcome.Negative =>
                "negative failed worsened ineffective",
            ExperimentStatus.Completed when entity.Result?.OutcomeClassification == ExperimentOutcome.Inconclusive =>
                "inconclusive unclear mixed",
            ExperimentStatus.Completed when entity.Result?.OutcomeClassification == ExperimentOutcome.Neutral =>
                "neutral no effect unchanged",
            ExperimentStatus.Abandoned => "abandoned discontinued stopped failed",
            ExperimentStatus.Active => "running active ongoing",
            _ => ""
        };

        EmbeddingFormatHelper.AppendKeywords(sb,
            "experiment", "hypothesis", "A/B test", "behavior change", "learning",
            "measurement", "intervention", "test", "trial", "outcome", outcomeKeywords);

        return sb.ToString();
    }

    private static string FormatEffectiveness(ExperimentOutcome outcome) => outcome switch
    {
        ExperimentOutcome.Positive => "Effective - hypothesis confirmed",
        ExperimentOutcome.Negative => "Ineffective - hypothesis rejected",
        ExperimentOutcome.Inconclusive => "Inconclusive - needs more data or different approach",
        ExperimentOutcome.Neutral => "No effect - hypothesis neither confirmed nor rejected",
        _ => "Unknown"
    };

    private static string FormatExperimentStatus(ExperimentStatus status) => status switch
    {
        ExperimentStatus.Draft => "Draft",
        ExperimentStatus.Active => "Active (Running)",
        ExperimentStatus.Paused => "Paused",
        ExperimentStatus.Completed => "Completed",
        ExperimentStatus.Abandoned => "Abandoned",
        ExperimentStatus.Archived => "Archived",
        _ => EmbeddingFormatHelper.FormatEnum(status)
    };
}
