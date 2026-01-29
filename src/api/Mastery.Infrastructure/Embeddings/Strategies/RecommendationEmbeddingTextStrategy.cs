using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for Recommendation entities.
/// Context depth: Self-contained (Target includes EntityTitle).
/// Excludes: Trace, RunHistory, internal scores, timestamps.
/// </summary>
public sealed class RecommendationEmbeddingTextStrategy : IEmbeddingTextStrategy<Recommendation>
{
    public Task<string?> CompileTextAsync(Recommendation entity, CancellationToken ct)
    {
        // Don't embed expired or executed recommendations - they're historical
        if (entity.Status is RecommendationStatus.Expired or RecommendationStatus.Executed)
        {
            return Task.FromResult<string?>(null);
        }

        var sb = new StringBuilder();

        // Build leading summary: "{Type}: {Title} - {ActionSummary}"
        var typeText = FormatRecommendationType(entity.Type);
        var actionPart = !string.IsNullOrWhiteSpace(entity.ActionSummary)
            ? $" - {entity.ActionSummary}"
            : "";
        EmbeddingFormatHelper.AppendSummary(sb, "RECOMMENDATION",
            $"{typeText}: {entity.Title}{actionPart}");

        // Core recommendation info
        sb.AppendLine($"Title: {entity.Title}");
        sb.AppendLine($"Type: {typeText}");
        sb.AppendLine($"Context: {FormatContext(entity.Context)}");
        sb.AppendLine($"Status: {FormatStatus(entity.Status)}");

        // Target information
        sb.AppendLine();
        sb.AppendLine($"Target: {EmbeddingFormatHelper.FormatEnum(entity.Target.Kind)}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Target entity", entity.Target.EntityTitle);

        // Action details
        sb.AppendLine();
        sb.AppendLine($"Suggested action: {FormatActionKind(entity.ActionKind)}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Action summary", entity.ActionSummary);

        // Rationale is critical for RAG - explains WHY
        sb.AppendLine();
        sb.AppendLine($"Rationale: {entity.Rationale}");

        // Include dismiss reason if available (useful for learning what doesn't work)
        if (entity.Status == RecommendationStatus.Dismissed && !string.IsNullOrWhiteSpace(entity.DismissReason))
        {
            sb.AppendLine();
            sb.AppendLine($"Dismiss reason: {entity.DismissReason}");
        }

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "recommendation", "suggestion", "next best action", "intervention",
            "coaching", "advice", "nudge", "prompt", "guidance");

        return Task.FromResult<string?>(sb.ToString());
    }

    private static string FormatRecommendationType(RecommendationType type) => type switch
    {
        RecommendationType.NextBestAction => "Next Best Action",
        RecommendationType.Top1Suggestion => "Top Priority Suggestion",
        RecommendationType.HabitModeSuggestion => "Habit Mode Suggestion",
        RecommendationType.PlanRealismAdjustment => "Plan Realism Adjustment",
        RecommendationType.TaskBreakdownSuggestion => "Task Breakdown Suggestion",
        RecommendationType.ScheduleAdjustmentSuggestion => "Schedule Adjustment",
        RecommendationType.ProjectStuckFix => "Project Stuck Fix",
        RecommendationType.ExperimentRecommendation => "Experiment Recommendation",
        RecommendationType.GoalScoreboardSuggestion => "Goal Scoreboard Suggestion",
        RecommendationType.HabitFromLeadMetricSuggestion => "Habit from Lead Metric",
        RecommendationType.CheckInConsistencyNudge => "Check-In Consistency Nudge",
        RecommendationType.MetricObservationReminder => "Metric Observation Reminder",
        RecommendationType.TaskEditSuggestion => "Task Edit Suggestion",
        RecommendationType.TaskArchiveSuggestion => "Task Archive Suggestion",
        RecommendationType.HabitEditSuggestion => "Habit Edit Suggestion",
        RecommendationType.HabitArchiveSuggestion => "Habit Archive Suggestion",
        RecommendationType.GoalEditSuggestion => "Goal Edit Suggestion",
        RecommendationType.GoalArchiveSuggestion => "Goal Archive Suggestion",
        RecommendationType.ProjectSuggestion => "Project Suggestion",
        RecommendationType.ProjectEditSuggestion => "Project Edit Suggestion",
        _ => EmbeddingFormatHelper.FormatEnum(type)
    };

    private static string FormatContext(RecommendationContext context) => context switch
    {
        RecommendationContext.Onboarding => "Onboarding",
        RecommendationContext.MorningCheckIn => "Morning Check-In",
        RecommendationContext.Midday => "Midday",
        RecommendationContext.EveningCheckIn => "Evening Check-In",
        RecommendationContext.WeeklyReview => "Weekly Review",
        RecommendationContext.DriftAlert => "Drift Alert",
        RecommendationContext.ProactiveCheck => "Proactive Check",
        _ => EmbeddingFormatHelper.FormatEnum(context)
    };

    private static string FormatStatus(RecommendationStatus status) => status switch
    {
        RecommendationStatus.Pending => "Pending (awaiting response)",
        RecommendationStatus.Accepted => "Accepted",
        RecommendationStatus.Dismissed => "Dismissed",
        RecommendationStatus.Expired => "Expired",
        RecommendationStatus.Executed => "Executed",
        _ => EmbeddingFormatHelper.FormatEnum(status)
    };

    private static string FormatActionKind(RecommendationActionKind kind) => kind switch
    {
        RecommendationActionKind.Create => "Create new item",
        RecommendationActionKind.Update => "Update existing item",
        RecommendationActionKind.ExecuteToday => "Execute today",
        RecommendationActionKind.Defer => "Defer to later",
        RecommendationActionKind.Remove => "Remove/Archive",
        RecommendationActionKind.ReflectPrompt => "Reflection prompt",
        RecommendationActionKind.LearnPrompt => "Learning prompt",
        _ => EmbeddingFormatHelper.FormatEnum(kind)
    };
}
