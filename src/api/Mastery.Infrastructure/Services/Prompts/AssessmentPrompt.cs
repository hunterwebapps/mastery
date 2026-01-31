using System.Text;
using System.Text.Json.Serialization;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Stage 1: Situational Assessment.
/// Analyzes the user's holistic state and produces a structured assessment
/// that drives the strategy and generation stages.
/// </summary>
internal static class AssessmentPrompt
{
    public const string PromptVersion = "assessment-v1.2";
    public const string Model = "gpt-5-mini";
    public const string SchemaName = "situational_assessment";

    public static readonly BinaryData ResponseSchema = BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "capacityStatus": { "type": "string", "enum": ["overloaded", "stretched", "balanced", "underloaded"] },
            "energyTrend": { "type": "string", "enum": ["declining", "stable", "improving"] },
            "overallMomentum": { "type": "string", "enum": ["stalled", "slowing", "steady", "accelerating"] },
            "keyStrengths": { "type": "array", "items": { "type": "string" } },
            "keyRisks": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "area": { "type": "string" },
                  "detail": { "type": "string" },
                  "severity": { "type": "string", "enum": ["high", "medium", "low"] }
                },
                "required": ["area", "detail", "severity"],
                "additionalProperties": false
              }
            },
            "patterns": { "type": "array", "items": { "type": "string" } },
            "goalProgressSummary": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "goalTitle": { "type": "string" },
                  "momentum": { "type": "string", "enum": ["stalled", "slowing", "steady", "accelerating"] },
                  "bottleneck": { "type": "string" }
                },
                "required": ["goalTitle", "momentum", "bottleneck"],
                "additionalProperties": false
              }
            },
            "contextNotes": { "type": "string" }
          },
          "required": ["capacityStatus", "energyTrend", "overallMomentum", "keyStrengths", "keyRisks", "patterns", "goalProgressSummary", "contextNotes"],
          "additionalProperties": false
        }
        """);

    public static string BuildSystemPrompt(RecommendationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            You are a personal development analyst embedded in a system called Mastery.
            Your job is to deeply analyze a user's current state and produce a structured situational assessment.

            You must evaluate:
            1. Overall capacity — are they overloaded, stretched, balanced, or underloaded?
            2. Energy trend — is energy declining, stable, or improving based on recent check-ins?
            3. Overall momentum — are they stalled, slowing, steady, or accelerating toward their goals?
            4. Key strengths — what's working well (habits with high adherence, consistent check-ins, etc.)
            5. Key risks — what needs attention (dropping adherence, stuck projects, drifting metrics)
            6. Patterns — recurring themes (e.g., energy drops on certain days, tasks that keep rescheduling)
            7. Goal progress — for each active goal, assess momentum and identify the bottleneck
            8. User identity context — do goals and habits align with stated values and roles?
            9. Capacity constraints — is actual utilization within stated limits? Is intensity matching season type?

            Be specific and evidence-based. Reference actual numbers (adherence %, streak counts, task counts).
            Consider user's values, roles, and season context when assessing alignment and priorities.
            Do NOT make recommendations — that comes later. Focus purely on understanding the situation.
            """);

        sb.AppendLine();
        sb.AppendLine(GetContextInstructions(context));

        return sb.ToString();
    }

    public static string BuildUserPrompt(UserStateSnapshot state, RecommendationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# User State Snapshot (as of {state.Today:yyyy-MM-dd})");
        sb.AppendLine($"Check-in streak: {state.CheckInStreak} days");
        sb.AppendLine();

        SerializeProfile(sb, state.Profile);
        SerializeGoals(sb, state.Goals);
        SerializeHabits(sb, state.Habits);
        SerializeTasks(sb, state.Tasks, state.Today);
        SerializeProjects(sb, state.Projects);
        SerializeExperiments(sb, state.Experiments);
        SerializeCheckIns(sb, state.RecentCheckIns);
        SerializeMetrics(sb, state.MetricDefinitions);

        return sb.ToString();
    }

    private static string GetContextInstructions(RecommendationContext context) => context switch
    {
        RecommendationContext.MorningCheckIn => "CONTEXT: Morning check-in. Focus on today's capacity, current energy level, what's scheduled, and whether the day looks feasible.",
        RecommendationContext.EveningCheckIn => "CONTEXT: Evening check-in. Focus on what was accomplished vs planned, patterns in missed items, and energy trajectory.",
        RecommendationContext.WeeklyReview => "CONTEXT: Weekly review. Take a 7-day view. Analyze trends in goal progress, habit adherence, capacity utilization, and experiment outcomes.",
        RecommendationContext.DriftAlert => "CONTEXT: Drift alert triggered. A lead metric or key indicator has deviated significantly. Focus on identifying the root cause.",
        RecommendationContext.Midday => "CONTEXT: Midday check. Brief assessment of remaining capacity and energy for the rest of the day.",
        RecommendationContext.Onboarding => "CONTEXT: New user onboarding. Assess what they've set up so far and what foundational elements are missing.",
        RecommendationContext.ProactiveCheck => "CONTEXT: Proactive background check. Perform a holistic health assessment across all dimensions — goal momentum, habit adherence trends, task pipeline health, project progress, and capacity. Identify the highest-leverage area for improvement. This is not triggered by the user, so focus on what they might be missing.",
        _ => "CONTEXT: General assessment."
    };

    private static void SerializeProfile(StringBuilder sb, UserProfileSnapshot? profile)
    {
        if (profile is null)
        {
            sb.AppendLine("## User Profile: Not available");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("## User Profile");
        sb.AppendLine($"Timezone: {profile.Timezone} | Locale: {profile.Locale}");
        sb.AppendLine();

        // Values
        if (profile.Values.Count > 0)
        {
            sb.AppendLine("### Core Values (ranked)");
            foreach (var v in profile.Values.Take(10))
                sb.AppendLine($"  {v.Rank}. {v.Label}{(v.Key is not null ? $" [{v.Key}]" : "")}");
            sb.AppendLine();
        }

        // Active Roles
        var activeRoles = profile.Roles.Where(r => r.IsActive).OrderByDescending(r => r.SeasonPriority).ThenBy(r => r.Rank).ToList();
        if (activeRoles.Count > 0)
        {
            sb.AppendLine("### Active Roles (by season priority)");
            foreach (var r in activeRoles)
                sb.AppendLine($"  - {r.Label} | SeasonPriority:{r.SeasonPriority}/5 | Min:{r.MinWeeklyMinutes}min/wk | Target:{r.TargetWeeklyMinutes}min/wk | Tags:[{string.Join(", ", r.Tags)}]");
            sb.AppendLine();
        }

        // Current Season
        if (profile.CurrentSeason is not null)
        {
            var s = profile.CurrentSeason;
            sb.AppendLine("### Current Season");
            sb.AppendLine($"  \"{s.Label}\" | Type:{s.Type} | Intensity:{s.Intensity}/10");
            sb.AppendLine($"  Period: {s.StartDate:yyyy-MM-dd} to {s.ExpectedEndDate?.ToString("yyyy-MM-dd") ?? "ongoing"}");
            if (!string.IsNullOrEmpty(s.SuccessStatement))
                sb.AppendLine($"  Success definition: \"{s.SuccessStatement}\"");
            if (s.NonNegotiables.Count > 0)
                sb.AppendLine($"  Non-negotiables: {string.Join(", ", s.NonNegotiables)}");
            if (s.FocusGoalIds.Count > 0)
                sb.AppendLine($"  Focus goals: {s.FocusGoalIds.Count} designated");
            sb.AppendLine();
        }

        // Preferences
        sb.AppendLine("### Preferences");
        sb.AppendLine($"  Coaching style: {profile.Preferences.CoachingStyle} | Verbosity: {profile.Preferences.Verbosity} | Nudge level: {profile.Preferences.NudgeLevel}");
        sb.AppendLine();

        // Constraints
        sb.AppendLine("### Capacity Constraints");
        sb.AppendLine($"  Weekday max: {profile.Constraints.MaxPlannedMinutesWeekday}min | Weekend max: {profile.Constraints.MaxPlannedMinutesWeekend}min");
        if (!string.IsNullOrEmpty(profile.Constraints.HealthNotes))
            sb.AppendLine($"  Health context: {profile.Constraints.HealthNotes}");
        if (profile.Constraints.ContentBoundaries.Count > 0)
            sb.AppendLine($"  Content boundaries: {string.Join(", ", profile.Constraints.ContentBoundaries)}");
        sb.AppendLine();
    }

    private static void SerializeGoals(StringBuilder sb, IReadOnlyList<GoalSnapshot> goals)
    {
        var active = goals.Where(g => g.Status == GoalStatus.Active).ToList();
        if (active.Count == 0) { sb.AppendLine("## Goals: None active"); sb.AppendLine(); return; }

        sb.AppendLine($"## Goals ({active.Count} active)");
        foreach (var g in active)
        {
            sb.AppendLine($"- [{g.Id}] \"{g.Title}\" | Priority:{g.Priority} | Deadline:{g.Deadline?.ToString("yyyy-MM-dd") ?? "none"}");
            foreach (var m in g.Metrics)
            {
                sb.AppendLine($"  - {m.Kind} metric: \"{m.MetricName}\" | Target:{m.TargetValue} | Current:{m.CurrentValue?.ToString() ?? "?"} | Source:{m.SourceHint}");
            }
        }
        sb.AppendLine();
    }

    private static void SerializeHabits(StringBuilder sb, IReadOnlyList<HabitSnapshot> habits)
    {
        var active = habits.Where(h => h.Status == HabitStatus.Active).ToList();
        if (active.Count == 0) { sb.AppendLine("## Habits: None active"); sb.AppendLine(); return; }

        sb.AppendLine($"## Habits ({active.Count} active)");
        foreach (var h in active)
        {
            sb.AppendLine($"- [{h.Id}] \"{h.Title}\" | Mode:{h.CurrentMode} | Adherence7d:{h.Adherence7Day:P0} | Streak:{h.CurrentStreak} | MetricBound:{(h.MetricBindingIds.Count > 0 ? "yes" : "no")}");
        }
        sb.AppendLine();
    }

    private static void SerializeTasks(StringBuilder sb, IReadOnlyList<TaskSnapshot> tasks, DateOnly today)
    {
        var actionable = tasks
            .Where(t => t.Status != Domain.Enums.TaskStatus.Completed &&
                        t.Status != Domain.Enums.TaskStatus.Cancelled &&
                        t.Status != Domain.Enums.TaskStatus.Archived)
            .OrderBy(t => t.Priority)
            .Take(15)
            .ToList();

        var todayTasks = actionable.Where(t => t.ScheduledDate == today).ToList();
        var totalMinutes = todayTasks.Sum(t => t.EstMinutes ?? 0);

        sb.AppendLine($"## Tasks ({actionable.Count} actionable, {todayTasks.Count} scheduled today, {totalMinutes} min total today)");
        foreach (var t in actionable)
        {
            var scheduled = t.ScheduledDate == today ? "Scheduled(today)" : t.ScheduledDate?.ToString("MMM dd") ?? "";
            var due = t.DueDate is not null ? $"Due:{t.DueDate:MMM dd}" : "";
            var goal = t.GoalId is not null ? "Goal-linked" : "";
            var reschedules = t.RescheduleCount > 0 ? $"Rescheduled:{t.RescheduleCount}x" : "";

            sb.AppendLine($"- [{t.Id}] \"{t.Title}\" | {t.Status} | P{t.Priority} | Energy:{t.EnergyLevel} | Est:{t.EstMinutes ?? 0}min | {scheduled} {due} {goal} {reschedules}".TrimEnd());
        }
        if (tasks.Count > 15) sb.AppendLine($"  ... and {tasks.Count - 15} more tasks");
        sb.AppendLine();
    }

    private static void SerializeProjects(StringBuilder sb, IReadOnlyList<ProjectSnapshot> projects)
    {
        var active = projects.Where(p => p.Status == ProjectStatus.Active).ToList();
        if (active.Count == 0) { sb.AppendLine("## Projects: None active"); sb.AppendLine(); return; }

        sb.AppendLine($"## Projects ({active.Count} active)");
        foreach (var p in active)
        {
            var nextAction = p.NextTaskId is not null ? "HasNextAction" : "NO_NEXT_ACTION";
            sb.AppendLine($"- [{p.Id}] \"{p.Title}\" | Progress:{p.CompletedTasks}/{p.TotalTasks} | {nextAction} | End:{p.TargetEndDate?.ToString("yyyy-MM-dd") ?? "none"}");
        }
        sb.AppendLine();
    }

    private static void SerializeExperiments(StringBuilder sb, IReadOnlyList<ExperimentSnapshot> experiments)
    {
        if (experiments.Count == 0) { sb.AppendLine("## Experiments: None"); sb.AppendLine(); return; }

        var active = experiments.Where(e => e.Status == ExperimentStatus.Active).ToList();
        var completed = experiments.Where(e => e.Status == ExperimentStatus.Completed).ToList();

        sb.AppendLine($"## Experiments ({active.Count} active, {completed.Count} completed)");
        foreach (var e in active)
            sb.AppendLine($"- [Active] [{e.Id}] \"{e.Title}\" | Started:{e.StartDate?.ToString("yyyy-MM-dd") ?? "?"}");
        foreach (var e in completed.Take(3))
            sb.AppendLine($"- [Completed] \"{e.Title}\"");
        sb.AppendLine();
    }

    private static void SerializeCheckIns(StringBuilder sb, IReadOnlyList<CheckInSnapshot> checkIns)
    {
        if (checkIns.Count == 0) { sb.AppendLine("## Recent Check-ins: None"); sb.AppendLine(); return; }

        sb.AppendLine($"## Recent Check-ins ({checkIns.Count} in window)");
        foreach (var c in checkIns.OrderByDescending(c => c.Date).Take(7))
        {
            var energy = c.EnergyLevel is not null ? $"Energy:{c.EnergyLevel}/5" : "";
            var top1 = c.Top1EntityId is not null
                ? $"Top1:{c.Top1Type}({(c.Top1Completed == true ? "completed" : "not completed")})"
                : "";
            sb.AppendLine($"- {c.Date:yyyy-MM-dd} | {c.Type} | {c.Status} | {energy} {top1}".TrimEnd());
        }
        sb.AppendLine();
    }

    private static void SerializeMetrics(StringBuilder sb, IReadOnlyList<MetricDefinitionSnapshot> metrics)
    {
        if (metrics.Count == 0) return;
        var manual = metrics.Where(m => m.SourceType == MetricSourceType.Manual).ToList();
        if (manual.Count == 0) return;

        sb.AppendLine($"## Manual Metrics ({manual.Count})");
        foreach (var m in manual)
        {
            var lastObs = m.LastObservationDate?.ToString("yyyy-MM-dd") ?? "never";
            sb.AppendLine($"- [{m.Id}] \"{m.Name}\" | LastObserved:{lastObs}");
        }
        sb.AppendLine();
    }
}

// Stage 1 response model
internal sealed class SituationalAssessment
{
    [JsonPropertyName("capacityStatus")]
    public string CapacityStatus { get; set; } = "balanced";

    [JsonPropertyName("energyTrend")]
    public string EnergyTrend { get; set; } = "stable";

    [JsonPropertyName("overallMomentum")]
    public string OverallMomentum { get; set; } = "steady";

    [JsonPropertyName("keyStrengths")]
    public List<string> KeyStrengths { get; set; } = [];

    [JsonPropertyName("keyRisks")]
    public List<AssessmentRisk> KeyRisks { get; set; } = [];

    [JsonPropertyName("patterns")]
    public List<string> Patterns { get; set; } = [];

    [JsonPropertyName("goalProgressSummary")]
    public List<GoalProgressEntry> GoalProgressSummary { get; set; } = [];

    [JsonPropertyName("contextNotes")]
    public string ContextNotes { get; set; } = "";
}

internal sealed class AssessmentRisk
{
    [JsonPropertyName("area")]
    public string Area { get; set; } = "";

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = "";

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "medium";
}

internal sealed class GoalProgressEntry
{
    [JsonPropertyName("goalTitle")]
    public string GoalTitle { get; set; } = "";

    [JsonPropertyName("momentum")]
    public string Momentum { get; set; } = "steady";

    [JsonPropertyName("bottleneck")]
    public string Bottleneck { get; set; } = "";
}
