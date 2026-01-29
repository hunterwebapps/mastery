using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Services;

public sealed class UserStateAssembler : IUserStateAssembler
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly IHabitRepository _habitRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICheckInRepository _checkInRepository;
    private readonly IExperimentRepository _experimentRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly IMetricObservationRepository _metricObservationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UserStateAssembler(
        IUserProfileRepository userProfileRepository,
        IGoalRepository goalRepository,
        IHabitRepository habitRepository,
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        ICheckInRepository checkInRepository,
        IExperimentRepository experimentRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        IMetricObservationRepository metricObservationRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _userProfileRepository = userProfileRepository;
        _goalRepository = goalRepository;
        _habitRepository = habitRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _checkInRepository = checkInRepository;
        _experimentRepository = experimentRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _metricObservationRepository = metricObservationRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<UserStateSnapshot> AssembleAsync(string userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var sevenDaysAgo = today.AddDays(-7);

        // Fetch all data sequentially (EF Core DbContext is not thread-safe)
        var userProfile = await _userProfileRepository.GetByUserIdWithSeasonAsync(userId, cancellationToken);
        var goals = await _goalRepository.GetActiveGoalsByUserIdAsync(userId, cancellationToken);
        var habits = await _habitRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        var tasks = await _taskRepository.GetByUserIdAsync(userId, cancellationToken);
        var projects = await _projectRepository.GetByUserIdAsync(userId, cancellationToken);
        var experiments = await _experimentRepository.GetByUserIdAsync(userId, cancellationToken);
        var checkIns = await _checkInRepository.GetByUserIdAndDateRangeAsync(userId, sevenDaysAgo, today, cancellationToken);
        var metricDefs = await _metricDefinitionRepository.GetByUserIdAsync(userId, false, cancellationToken);
        var adherenceRates = await _habitRepository.GetAdherenceRatesAsync(userId, 7, cancellationToken);
        var streaks = await _habitRepository.GetStreaksAsync(userId, cancellationToken);
        var checkInStreak = await _checkInRepository.CalculateStreakAsync(userId, today, cancellationToken);

        // Determine source types from goal metric bindings
        var goalMetricSourceHints = goals
            .SelectMany(g => g.Metrics)
            .GroupBy(gm => gm.MetricDefinitionId)
            .ToDictionary(g => g.Key, g => g.First().SourceHint);

        // Get latest observation dates for metrics
        var metricSnapshots = new List<MetricDefinitionSnapshot>();
        foreach (var md in metricDefs)
        {
            var sourceType = goalMetricSourceHints.GetValueOrDefault(md.Id, MetricSourceType.Manual);
            DateTime? lastObs = null;
            if (sourceType == MetricSourceType.Manual)
            {
                var latest = await _metricObservationRepository.GetLatestByMetricAsync(md.Id, userId, cancellationToken);
                lastObs = latest?.CreatedAt;
            }
            metricSnapshots.Add(new MetricDefinitionSnapshot(
                md.Id,
                md.Name,
                md.Description,
                md.DataType.ToString(),
                md.Direction.ToString(),
                sourceType,
                lastObs,
                // Extended fields
                md.Unit?.UnitType,
                md.Unit?.DisplayLabel,
                md.DefaultCadence.ToString(),
                md.DefaultAggregation.ToString(),
                md.Tags.ToList()));
        }

        // Get current metric values for goal metrics
        var goalSnapshots = new List<GoalSnapshot>();
        foreach (var goal in goals)
        {
            var metricSnaps = new List<GoalMetricSnapshot>();
            foreach (var gm in goal.Metrics)
            {
                var metricDef = metricDefs.FirstOrDefault(m => m.Id == gm.MetricDefinitionId);
                decimal? currentValue = await _metricObservationRepository.GetAggregatedValueAsync(
                    gm.MetricDefinitionId, userId, sevenDaysAgo, today, gm.Aggregation, cancellationToken);

                metricSnaps.Add(new GoalMetricSnapshot(
                    gm.Id,
                    gm.MetricDefinitionId,
                    metricDef?.Name ?? "Unknown",
                    gm.Kind,
                    gm.Target.Value,
                    currentValue,
                    gm.SourceHint,
                    // Extended fields
                    gm.Target.Type.ToString(),
                    gm.Target.MaxValue,
                    gm.EvaluationWindow.Type.ToString(),
                    gm.EvaluationWindow.RollingDays,
                    gm.Aggregation.ToString(),
                    gm.Weight,
                    gm.Baseline));
            }

            goalSnapshots.Add(new GoalSnapshot(
                goal.Id, goal.Title, goal.Status, goal.Priority, goal.Deadline, metricSnaps));
        }

        // Map habits
        var habitSnapshots = habits.Select(h => new HabitSnapshot(
            h.Id,
            h.Title,
            h.Status,
            h.DefaultMode,
            adherenceRates.GetValueOrDefault(h.Id, 0m),
            streaks.GetValueOrDefault(h.Id, 0),
            h.MetricBindings.Select(b => b.MetricDefinitionId).ToList(),
            Schedule: h.Schedule is not null
                ? new HabitScheduleSnapshot(
                    h.Schedule.Type.ToString(),
                    h.Schedule.DaysOfWeek?.Select(d => (int)d).ToArray(),
                    h.Schedule.FrequencyPerWeek,
                    h.Schedule.IntervalDays)
                : null,
            Variants: h.Variants.Count > 0
                ? h.Variants.Select(v => new HabitVariantSnapshot(
                    v.Mode.ToString(),
                    v.Label,
                    v.EstimatedMinutes,
                    v.EnergyCost)).ToList()
                : null,
            GoalIds: h.GoalIds.Count > 0 ? h.GoalIds.ToList() : null))
            .ToList();

        // Map tasks (only non-archived, non-cancelled)
        var taskSnapshots = tasks
            .Where(t => t.Status != Domain.Enums.TaskStatus.Archived && t.Status != Domain.Enums.TaskStatus.Cancelled)
            .Select(t => new TaskSnapshot(
                t.Id, t.Title, t.Status,
                t.EstimatedMinutes, t.EnergyCost, t.Priority,
                t.ProjectId, t.GoalId,
                t.Scheduling?.ScheduledOn, t.Due?.DueOn, t.Due?.DueType,
                t.RescheduleCount,
                t.ContextTags.Select(ct => ct.ToString()).ToList()))
            .ToList();

        // Map projects (only active) — compute task counts from loaded tasks
        var projectSnapshots = projects
            .Where(p => p.Status == ProjectStatus.Active)
            .Select(p =>
            {
                var projectTasks = taskSnapshots.Where(t => t.ProjectId == p.Id).ToList();
                return new ProjectSnapshot(
                    p.Id, p.Title, p.Status, p.GoalId, p.NextTaskId,
                    projectTasks.Count,
                    projectTasks.Count(t => t.Status == Domain.Enums.TaskStatus.Completed),
                    p.TargetEndDate,
                    p.Priority,
                    Milestones: p.Milestones.Count > 0
                        ? p.Milestones
                            .OrderBy(m => m.DisplayOrder)
                            .Select(m => new MilestoneSnapshot(
                                m.Id,
                                m.Title,
                                m.Status.ToString(),
                                m.TargetDate))
                            .ToList()
                        : null);
            })
            .ToList();

        // Map experiments with full details
        var experimentSnapshots = experiments.Select(e => new ExperimentSnapshot(
            e.Id,
            e.Title,
            e.Status,
            e.StartDate,
            e.EndDatePlanned,
            // Extended fields
            e.Category.ToString(),
            e.LinkedGoalIds.ToList(),
            e.Hypothesis is not null
                ? new ExperimentHypothesisSnapshot(
                    e.Hypothesis.Change,
                    e.Hypothesis.ExpectedOutcome,
                    e.Hypothesis.Rationale)
                : null,
            e.MeasurementPlan is not null
                ? new ExperimentMeasurementPlanSnapshot(
                    e.MeasurementPlan.PrimaryMetricDefinitionId,
                    e.MeasurementPlan.PrimaryAggregation.ToString(),
                    e.MeasurementPlan.BaselineWindowDays,
                    e.MeasurementPlan.RunWindowDays,
                    e.MeasurementPlan.GuardrailMetricDefinitionIds.ToList())
                : null))
            .ToList();

        // Map check-ins
        var checkInSnapshots = checkIns.Select(c => new CheckInSnapshot(
            c.Id, c.CheckInDate, c.Type, c.Status,
            c.EnergyLevel, c.Top1Type?.ToString(), c.Top1EntityId, c.Top1Completed))
            .ToList();

        // Map user profile (if exists)
        UserProfileSnapshot? profileSnapshot = null;
        if (userProfile is not null)
        {
            profileSnapshot = new UserProfileSnapshot(
                Timezone: userProfile.Timezone.IanaId,
                Locale: userProfile.Locale.Code,
                Values: userProfile.Values
                    .OrderBy(v => v.Rank)
                    .Select(v => new UserValueSnapshot(v.Label, v.Key, v.Rank))
                    .ToList(),
                Roles: userProfile.Roles
                    .Select(r => new UserRoleSnapshot(
                        r.Id, r.Label, r.Key, r.Rank, r.SeasonPriority,
                        r.MinWeeklyMinutes, r.TargetWeeklyMinutes,
                        r.Tags, r.Status == Domain.Entities.UserProfile.RoleStatus.Active))
                    .ToList(),
                CurrentSeason: userProfile.CurrentSeason is not null
                    ? new SeasonSnapshot(
                        userProfile.CurrentSeason.Id,
                        userProfile.CurrentSeason.Label,
                        userProfile.CurrentSeason.Type.ToString(),
                        userProfile.CurrentSeason.Intensity,
                        userProfile.CurrentSeason.SuccessStatement,
                        userProfile.CurrentSeason.NonNegotiables.ToList(),
                        userProfile.CurrentSeason.FocusRoleIds.ToList(),
                        userProfile.CurrentSeason.FocusGoalIds.ToList(),
                        userProfile.CurrentSeason.StartDate,
                        userProfile.CurrentSeason.ExpectedEndDate)
                    : null,
                Preferences: new PreferencesSnapshot(
                    userProfile.Preferences.CoachingStyle.ToString(),
                    userProfile.Preferences.ExplanationVerbosity.ToString(),
                    userProfile.Preferences.NudgeLevel.ToString()),
                Constraints: new ConstraintsSnapshot(
                    userProfile.Constraints.MaxPlannedMinutesWeekday,
                    userProfile.Constraints.MaxPlannedMinutesWeekend,
                    userProfile.Constraints.HealthNotes,
                    userProfile.Constraints.ContentBoundaries));
        }

        return new UserStateSnapshot(
            userId, profileSnapshot, goalSnapshots, habitSnapshots, taskSnapshots,
            projectSnapshots, experimentSnapshots, checkInSnapshots,
            metricSnapshots, checkInStreak, today);
    }
}
