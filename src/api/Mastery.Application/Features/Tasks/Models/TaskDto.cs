namespace Mastery.Application.Features.Tasks.Models;

/// <summary>
/// Full task DTO with all details including metric bindings.
/// </summary>
public sealed record TaskDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectTitle { get; init; }
    public Guid? GoalId { get; init; }
    public string? GoalTitle { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public int Priority { get; init; }
    public int EstimatedMinutes { get; init; }
    public int EnergyCost { get; init; }
    public int DisplayOrder { get; init; }
    public TaskDueDto? Due { get; init; }
    public TaskSchedulingDto? Scheduling { get; init; }
    public TaskCompletionDto? Completion { get; init; }
    public List<string> ContextTags { get; init; } = [];
    public List<Guid> DependencyTaskIds { get; init; } = [];
    public List<Guid> RoleIds { get; init; } = [];
    public List<Guid> ValueIds { get; init; } = [];
    public List<TaskMetricBindingDto> MetricBindings { get; init; } = [];
    public string? LastRescheduleReason { get; init; }
    public int RescheduleCount { get; init; }
    public bool IsOverdue { get; init; }
    public bool IsBlocked { get; init; }
    public bool IsEligibleForNBA { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Task summary for list views - lightweight without nested details.
/// </summary>
public sealed record TaskSummaryDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public int Priority { get; init; }
    public int EstimatedMinutes { get; init; }
    public int EnergyCost { get; init; }
    public int DisplayOrder { get; init; }
    public string? DueOn { get; init; }
    public string? DueType { get; init; }
    public string? ScheduledOn { get; init; }
    public List<string> ContextTags { get; init; } = [];
    public bool IsOverdue { get; init; }
    public bool IsBlocked { get; init; }
    public bool HasDependencies { get; init; }
    public int RescheduleCount { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectTitle { get; init; }
    public Guid? GoalId { get; init; }
    public string? GoalTitle { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Optimized projection for Today view - critical for daily loop.
/// Includes project/goal context for quick reference.
/// </summary>
public sealed record TodayTaskDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public int Priority { get; init; }
    public int EstimatedMinutes { get; init; }
    public int EnergyCost { get; init; }
    public int DisplayOrder { get; init; }
    public string? DueOn { get; init; }
    public string? DueType { get; init; }
    public string? ScheduledOn { get; init; }
    public List<string> ContextTags { get; init; } = [];
    public bool IsOverdue { get; init; }
    public bool IsBlocked { get; init; }
    public bool RequiresValueEntry { get; init; }
    public int RescheduleCount { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectTitle { get; init; }
    public Guid? GoalId { get; init; }
    public string? GoalTitle { get; init; }
    public List<Guid> DependencyTaskIds { get; init; } = [];
}

/// <summary>
/// Due date configuration for a task.
/// </summary>
public sealed record TaskDueDto
{
    public string? DueOn { get; init; }
    public string? DueAt { get; init; }
    public required string DueType { get; init; }
}

/// <summary>
/// Scheduling configuration for a task.
/// </summary>
public sealed record TaskSchedulingDto
{
    public required string ScheduledOn { get; init; }
    public string? PreferredTimeWindowStart { get; init; }
    public string? PreferredTimeWindowEnd { get; init; }
}

/// <summary>
/// Completion data for a completed task.
/// </summary>
public sealed record TaskCompletionDto
{
    public required DateTime CompletedAtUtc { get; init; }
    public required string CompletedOn { get; init; }
    public int? ActualMinutes { get; init; }
    public string? CompletionNote { get; init; }
    public decimal? EnteredValue { get; init; }
}

/// <summary>
/// Metric binding for a task.
/// </summary>
public sealed record TaskMetricBindingDto
{
    public Guid Id { get; init; }
    public Guid MetricDefinitionId { get; init; }
    public string? MetricName { get; init; }
    public required string ContributionType { get; init; }
    public decimal? FixedValue { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Task inbox item for quick capture view.
/// </summary>
public sealed record InboxTaskDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public int EstimatedMinutes { get; init; }
    public int EnergyCost { get; init; }
    public List<string> ContextTags { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Batch operation result for multiple task operations.
/// </summary>
public sealed record BatchOperationResultDto
{
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<Guid> SuccessIds { get; init; } = [];
    public List<BatchFailureDto> Failures { get; init; } = [];
}

/// <summary>
/// Individual failure in a batch operation.
/// </summary>
public sealed record BatchFailureDto
{
    public Guid TaskId { get; init; }
    public required string Error { get; init; }
}

/// <summary>
/// Task statistics and analytics.
/// </summary>
public sealed record TaskStatsDto
{
    public int TotalCompleted { get; init; }
    public int TotalCancelled { get; init; }
    public int TotalPending { get; init; }
    public int TotalRescheduled { get; init; }
    public int TotalMinutesCompleted { get; init; }
    public Dictionary<string, int> CompletionsByDayOfWeek { get; init; } = new();
    public Dictionary<string, int> RescheduleReasonDistribution { get; init; } = new();
    public Dictionary<string, int> CompletionsByContextTag { get; init; } = new();
}
