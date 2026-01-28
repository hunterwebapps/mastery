namespace Mastery.Api.Contracts.Tasks;

/// <summary>
/// Request to create a new task.
/// </summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description = null,
    int EstimatedMinutes = 30,
    int EnergyCost = 3,
    int Priority = 3,
    Guid? ProjectId = null,
    Guid? GoalId = null,
    CreateTaskDueRequest? Due = null,
    CreateTaskSchedulingRequest? Scheduling = null,
    List<string>? ContextTags = null,
    List<Guid>? DependencyTaskIds = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<CreateTaskMetricBindingRequest>? MetricBindings = null,
    bool StartAsReady = false);

/// <summary>
/// Request to create a task due date.
/// </summary>
public sealed record CreateTaskDueRequest(
    string DueOn,
    string? DueAt = null,
    string DueType = "Soft");

/// <summary>
/// Request to create a task scheduling.
/// </summary>
public sealed record CreateTaskSchedulingRequest(
    string ScheduledOn,
    string? PreferredTimeWindowStart = null,
    string? PreferredTimeWindowEnd = null);

/// <summary>
/// Request to create a task metric binding.
/// </summary>
public sealed record CreateTaskMetricBindingRequest(
    Guid MetricDefinitionId,
    string ContributionType,
    decimal? FixedValue = null,
    string? Notes = null);

/// <summary>
/// Request to update a task.
/// </summary>
public sealed record UpdateTaskRequest(
    string? Title = null,
    string? Description = null,
    int? EstimatedMinutes = null,
    int? EnergyCost = null,
    int? Priority = null,
    Guid? ProjectId = null,
    Guid? GoalId = null,
    CreateTaskDueRequest? Due = null,
    List<string>? ContextTags = null,
    List<Guid>? DependencyTaskIds = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null);

/// <summary>
/// Request to schedule a task.
/// </summary>
public sealed record ScheduleTaskRequest(
    string ScheduledOn,
    string? PreferredTimeWindowStart = null,
    string? PreferredTimeWindowEnd = null);

/// <summary>
/// Request to reschedule a task.
/// </summary>
public sealed record RescheduleTaskRequest(
    string NewDate,
    string? Reason = null);

/// <summary>
/// Request to complete a task.
/// </summary>
public sealed record CompleteTaskRequest(
    string CompletedOn,
    int? ActualMinutes = null,
    string? Note = null,
    decimal? EnteredValue = null);

/// <summary>
/// Request to batch complete multiple tasks.
/// </summary>
public sealed record BatchCompleteTasksRequest(
    List<BatchCompleteTaskItem> Items);

/// <summary>
/// Item in a batch complete request.
/// </summary>
public sealed record BatchCompleteTaskItem(
    Guid TaskId,
    string CompletedOn,
    int? ActualMinutes = null,
    decimal? EnteredValue = null);

/// <summary>
/// Request to batch reschedule multiple tasks.
/// </summary>
public sealed record BatchRescheduleTasksRequest(
    List<Guid> TaskIds,
    string NewDate,
    string? Reason = null);

/// <summary>
/// Request to batch cancel multiple tasks.
/// </summary>
public sealed record BatchCancelTasksRequest(
    List<Guid> TaskIds);
