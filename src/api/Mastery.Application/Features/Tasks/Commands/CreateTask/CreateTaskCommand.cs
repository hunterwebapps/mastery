using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Creates a new task for the current user.
/// </summary>
public sealed record CreateTaskCommand(
    string Title,
    string? Description = null,
    int EstimatedMinutes = 30,
    int EnergyCost = 3,
    int Priority = 3,
    Guid? ProjectId = null,
    Guid? GoalId = null,
    CreateTaskDueInput? Due = null,
    CreateTaskSchedulingInput? Scheduling = null,
    List<string>? ContextTags = null,
    List<Guid>? DependencyTaskIds = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<CreateTaskMetricBindingInput>? MetricBindings = null,
    bool StartAsReady = false) : ICommand<Guid>;

/// <summary>
/// Input for creating a task due date configuration.
/// </summary>
public sealed record CreateTaskDueInput(
    string DueOn,
    string? DueAt = null,
    string DueType = "Soft");

/// <summary>
/// Input for creating a task scheduling configuration.
/// </summary>
public sealed record CreateTaskSchedulingInput(
    string ScheduledOn,
    string? PreferredTimeWindowStart = null,
    string? PreferredTimeWindowEnd = null);

/// <summary>
/// Input for creating a task metric binding.
/// </summary>
public sealed record CreateTaskMetricBindingInput(
    Guid MetricDefinitionId,
    string ContributionType,
    decimal? FixedValue = null,
    string? Notes = null);
