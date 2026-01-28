namespace Mastery.Application.Features.Projects.Models;

/// <summary>
/// Full project DTO with all details including milestones.
/// </summary>
public sealed record ProjectDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public int Priority { get; init; }
    public Guid? GoalId { get; init; }
    public string? GoalTitle { get; init; }
    public Guid? SeasonId { get; init; }
    public string? TargetEndDate { get; init; }
    public Guid? NextTaskId { get; init; }
    public string? NextTaskTitle { get; init; }
    public List<Guid> RoleIds { get; init; } = [];
    public List<Guid> ValueIds { get; init; } = [];
    public List<MilestoneDto> Milestones { get; init; } = [];
    public string? OutcomeNotes { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int InProgressTasks { get; init; }
    public bool IsStuck { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Project summary for list views - lightweight with key metrics.
/// </summary>
public sealed record ProjectSummaryDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public int Priority { get; init; }
    public Guid? GoalId { get; init; }
    public string? GoalTitle { get; init; }
    public string? TargetEndDate { get; init; }
    public Guid? NextTaskId { get; init; }
    public string? NextTaskTitle { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int MilestoneCount { get; init; }
    public int CompletedMilestones { get; init; }
    public bool IsStuck { get; init; }
    public bool IsNearingDeadline { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Milestone within a project.
/// </summary>
public sealed record MilestoneDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public required string Title { get; init; }
    public string? TargetDate { get; init; }
    public required string Status { get; init; }
    public string? Notes { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Project with task counts by status.
/// </summary>
public sealed record ProjectDetailDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public int Priority { get; init; }
    public Guid? GoalId { get; init; }
    public string? GoalTitle { get; init; }
    public Guid? SeasonId { get; init; }
    public string? TargetEndDate { get; init; }
    public Guid? NextTaskId { get; init; }
    public string? NextTaskTitle { get; init; }
    public List<MilestoneDto> Milestones { get; init; } = [];
    public ProjectTaskCountsDto TaskCounts { get; init; } = new();
    public string? OutcomeNotes { get; init; }
    public bool IsStuck { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Task counts by status for a project.
/// </summary>
public sealed record ProjectTaskCountsDto
{
    public int Inbox { get; init; }
    public int Ready { get; init; }
    public int Scheduled { get; init; }
    public int InProgress { get; init; }
    public int Completed { get; init; }
    public int Cancelled { get; init; }
    public int Total { get; init; }
}

/// <summary>
/// Project progress overview for dashboard.
/// </summary>
public sealed record ProjectProgressDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int TotalMilestones { get; init; }
    public int CompletedMilestones { get; init; }
    public decimal CompletionPercentage { get; init; }
    public string? TargetEndDate { get; init; }
    public int? DaysUntilDeadline { get; init; }
}

/// <summary>
/// Stuck project (active but no next action).
/// </summary>
public sealed record StuckProjectDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public int Priority { get; init; }
    public int TotalReadyTasks { get; init; }
    public string? SuggestedNextTaskTitle { get; init; }
    public Guid? SuggestedNextTaskId { get; init; }
    public DateTime LastUpdated { get; init; }
}
