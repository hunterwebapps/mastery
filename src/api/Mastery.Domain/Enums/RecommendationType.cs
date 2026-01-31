namespace Mastery.Domain.Enums;

public enum RecommendationType
{
    // Action-based suggestions
    NextBestAction,
    Top1Suggestion,
    HabitModeSuggestion,
    PlanRealismAdjustment,
    TaskBreakdownSuggestion,
    ScheduleAdjustmentSuggestion,
    ProjectStuckFix,
    ExperimentRecommendation,
    GoalScoreboardSuggestion,
    HabitFromLeadMetricSuggestion,
    CheckInConsistencyNudge,
    MetricObservationReminder,

    // Task Edit/Archive/Triage suggestions
    TaskEditSuggestion,
    TaskArchiveSuggestion,
    TaskTriageSuggestion,

    // Habit Edit/Archive suggestions
    HabitEditSuggestion,
    HabitArchiveSuggestion,

    // Goal Edit/Archive suggestions
    GoalEditSuggestion,
    GoalArchiveSuggestion,

    // Project suggestions
    ProjectSuggestion,
    ProjectEditSuggestion,
    ProjectArchiveSuggestion,
    ProjectGoalLinkSuggestion,

    // Metric Edit suggestion
    MetricEditSuggestion,

    // Experiment Edit/Archive suggestions
    ExperimentEditSuggestion,
    ExperimentArchiveSuggestion
}
