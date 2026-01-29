namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Centralized schema/enum definitions for LLM prompts.
/// Used by domain generation prompts to provide consistent field reference documentation.
/// </summary>
internal static class SchemaReference
{
    /// <summary>
    /// ContextTag enum values with semantic meanings for task filtering.
    /// </summary>
    public const string ContextTagSchema = """
        ContextTags (array of strings, optional):
          - Computer: Requires laptop/desktop
          - Phone: Can be done from phone
          - Errands: Requires going out
          - Home: Best done at home
          - Office: Best done at workplace
          - DeepWork: Requires distraction-free focus
          - LowEnergy: Suitable for tired states
          - Anywhere: No location constraint
        """;

    /// <summary>
    /// Priority scale explanation (1-5).
    /// </summary>
    public const string PrioritySchema = """
        Priority (1-5):
          1 = Critical/urgent (do first)
          2 = High importance
          3 = Normal (default)
          4 = Low importance
          5 = Nice-to-have/someday
        """;

    /// <summary>
    /// EnergyCost scale explanation (1-5).
    /// </summary>
    public const string EnergyCostSchema = """
        EnergyCost (1-5):
          1 = Minimal effort (quick wins)
          2 = Light effort
          3 = Moderate effort (default)
          4 = Demanding
          5 = Exhausting (deep work)
        """;

    /// <summary>
    /// DueType explanation for task due dates.
    /// </summary>
    public const string DueTypeSchema = """
        DueType:
          - Soft: Gentle guidance, no failure if missed
          - Hard: Firm commitment, generates overdue signals
        Use Hard only for external deadlines or firm commitments.
        """;

    /// <summary>
    /// HabitMode explanation for graceful degradation.
    /// </summary>
    public const string HabitModeSchema = """
        HabitMode:
          - Full: Complete version (default)
          - Maintenance: Reduced version for moderate capacity
          - Minimum: Bare minimum to maintain streak
        Suggest Minimum when energy is low or adherence is dropping.
        """;

    /// <summary>
    /// Schedule type explanation for habits.
    /// </summary>
    public const string ScheduleTypeSchema = """
        Schedule Types:
          - Daily: Due every day
          - DaysOfWeek: Specific days (provide daysOfWeek array 0-6, 0=Sunday)
          - WeeklyFrequency: X times per week (provide frequencyPerWeek 1-7)
          - Interval: Every N days (provide intervalDays 1-90)
        """;

    /// <summary>
    /// Habit variant structure for graceful degradation.
    /// </summary>
    public const string HabitVariantSchema = """
        Habit Variants (for graceful degradation):
          - Full: Complete version (e.g., "30 min workout")
          - Maintenance: Reduced version (e.g., "15 min workout")
          - Minimum: Bare minimum (e.g., "5 min stretch")
        Each variant has: label, estimatedMinutes, energyCost
        """;

    /// <summary>
    /// Milestone structure for projects.
    /// </summary>
    public const string MilestoneSchema = """
        Milestones (for projects):
          - Title: Brief checkpoint name
          - TargetDate: Optional YYYY-MM-DD
        Projects with 3+ milestones have better completion rates.
        """;

    /// <summary>
    /// Task-specific field guidance.
    /// </summary>
    public const string TaskFieldGuidance = """
        Field Guidance:
          - Default to priority 3, energyCost 3 unless context suggests otherwise
          - Link tasks to projects/goals when connection is clear
          - Use contextTags to enable context-aware Next Best Action filtering
          - For roleIds/valueIds: only include if strongly aligned
        """;

    /// <summary>
    /// Habit-specific field guidance.
    /// </summary>
    public const string HabitFieldGuidance = """
        Field Guidance:
          - Start new habits with Full mode but suggest achievable durations
          - Link habits to goals via goalIds when they drive lead metrics
          - Suggest Daily or 3x/week schedules for new habits (build momentum)
          - Frame mode suggestions as protecting the streak, not giving up
        """;

    /// <summary>
    /// Project-specific field guidance.
    /// </summary>
    public const string ProjectFieldGuidance = """
        Field Guidance:
          - Projects are execution containers that group tasks toward a goal
          - Link to goals when possible for better tracking
          - Consider milestones for projects with >3 week timeline
          - Archive projects inactive >30 days or misaligned with current season
        """;

    // ─────────────────────────────────────────────────────────────────
    // Metric-related Schemas
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// MetricKind enum documentation for goal scoreboards.
    /// </summary>
    public const string MetricKindSchema = """
        MetricKind:
          - Lag: Outcome metric (what you're trying to achieve)
            Example: Weight, Revenue, Project Completion %
          - Lead: Leading indicator (predictive behaviors that drive outcomes)
            Example: Workout Sessions, Cold Calls, Study Hours
          - Constraint: Guardrail metric (what NOT to sacrifice)
            Example: Sleep Hours, Stress Level, Family Time
        Best practice: Each goal should have 1 Lag, 2 Lead, 1 Constraint
        """;

    /// <summary>
    /// MetricDataType enum documentation.
    /// </summary>
    public const string MetricDataTypeSchema = """
        MetricDataType:
          - Number: Numeric value (weight, revenue)
          - Boolean: Yes/no (did I meditate?)
          - Duration: Time in minutes (deep work time)
          - Percentage: 0-100% (completion rate)
          - Count: Occurrences (gym sessions, cold calls)
          - Rating: 1-5 scale (energy level, mood)
        """;

    /// <summary>
    /// MetricDirection enum documentation.
    /// </summary>
    public const string MetricDirectionSchema = """
        MetricDirection:
          - Increase: Higher is better (revenue, workout minutes)
          - Decrease: Lower is better (weight loss, expenses)
          - Maintain: Stay within range (sleep hours 7-9)
        """;

    /// <summary>
    /// MetricAggregation enum documentation.
    /// </summary>
    public const string MetricAggregationSchema = """
        MetricAggregation (how observations combine over window):
          - Sum: Total of all values (workout minutes/week)
          - Average: Mean of values (average sleep hours)
          - Max: Highest value in window
          - Min: Lowest value in window
          - Count: Number of observations (gym visits)
          - Latest: Most recent value (current weight)
        """;

    /// <summary>
    /// TargetType enum documentation.
    /// </summary>
    public const string TargetTypeSchema = """
        TargetType:
          - AtLeast: Value >= target (e.g., >= 30 min/day)
          - AtMost: Value <= target (e.g., <= 2 drinks/week)
          - Between: Value in range (e.g., sleep 7-9 hours)
          - Exactly: Precise match (rare, for fixed deliverables)
        """;

    /// <summary>
    /// WindowType enum documentation.
    /// </summary>
    public const string WindowTypeSchema = """
        WindowType (evaluation period):
          - Daily: Reset each day
          - Weekly: Monday-Sunday (or custom start)
          - Monthly: 1st to last of month
          - Rolling: Last N days (specify rollingDays)
        """;

    /// <summary>
    /// MetricSourceType enum documentation.
    /// </summary>
    public const string MetricSourceTypeSchema = """
        MetricSourceType (how observations are captured):
          - Manual: User enters observations directly
          - Habit: Derived from habit completions
          - Task: Derived from task completions
          - CheckIn: From check-in fields (energy, etc.)
          - Integration: External system import
          - Computed: Calculated from other metrics
        """;

    /// <summary>
    /// MetricUnit guidance for new metric definitions.
    /// </summary>
    public const string MetricUnitGuidance = """
        MetricUnit (for new metric definitions):
          Common units:
            - "min" (duration) for time-based metrics
            - "" (count) for counts and sessions
            - "%" for percentages
            - "/5" for ratings
            - "kg" or "lb" for weight
            - "$" for currency
          Provide: unitType (duration/count/percentage/rating/weight/currency/none)
                   displayLabel (the symbol shown: min, %, $, etc.)
        """;

    /// <summary>
    /// GoalMetric field guidance for scoreboard suggestions.
    /// </summary>
    public const string GoalMetricFieldGuidance = """
        GoalMetric Field Guidance:
          - kind: Choose based on metric's role (Lag=outcome, Lead=behavior, Constraint=guardrail)
          - targetType: Use AtLeast for minimums, AtMost for caps, Between for ranges
          - windowType: Match the natural measurement cadence (Daily for habits, Weekly for totals)
          - aggregation: Sum for totals, Average for means, Count for frequency
          - weight: 0.0-1.0, defaults to 1.0; reduce for secondary metrics
          - sourceHint: Manual unless metric is auto-populated from habits/tasks
        """;

    // ─────────────────────────────────────────────────────────────────
    // Experiment-related Schemas
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// ExperimentCategory enum documentation.
    /// </summary>
    public const string ExperimentCategorySchema = """
        ExperimentCategory:
          - Habit: Habit formation, scaling, modification
          - Routine: Schedule or routine changes
          - Environment: Context or environment changes
          - Mindset: Cognitive strategies, reframing
          - Productivity: Workflow or productivity tactics
          - Health: Energy or health management
          - Social: Relational or communication strategies
          - PlanRealism: Testing planning feasibility
          - FrictionReduction: Reducing barriers to action
          - CheckInConsistency: Improving check-in completion
          - Top1FollowThrough: Improving top-1 priority completion
          - Other: Doesn't fit other categories
        """;

    /// <summary>
    /// Hypothesis structure guidance for experiments.
    /// </summary>
    public const string HypothesisGuidance = """
        Hypothesis Structure:
          change: What the user will do differently (independent variable)
          expectedOutcome: What improvement is expected (dependent variable)
          rationale: Why this change should work (mechanism)
        Format: "If I {change}, then {expectedOutcome} because {rationale}"
        """;

    /// <summary>
    /// MeasurementPlan guidance for experiments.
    /// </summary>
    public const string MeasurementPlanGuidance = """
        MeasurementPlan:
          primaryMetricDefinitionId: The metric to track (required - use existing or create new)
          primaryAggregation: How to aggregate (Sum, Average, etc.)
          baselineWindowDays: Days before experiment to establish baseline (default: 7)
          runWindowDays: Duration of experiment in days (7-28, default: 14)
          guardrailMetricDefinitionIds: Optional metrics to monitor for side effects
        Tip: If no suitable metric exists, first create via newPrimaryMetric, then reference it.
        """;

    /// <summary>
    /// Experiment field guidance for generation.
    /// </summary>
    public const string ExperimentFieldGuidance = """
        Experiment Field Guidance:
          - Title: Clear, specific, time-bounded (e.g., "Sleep by 10pm for 2 weeks")
          - Category: Match the nature of the change being tested
          - Hypothesis: Be specific about change, outcome, and mechanism
          - MeasurementPlan: Always specify a primary metric to track
          - LinkedGoalIds: Connect to goals this experiment may impact
          - runWindowDays: 7-28 days; 14 is default for behavioral experiments
        """;
}
