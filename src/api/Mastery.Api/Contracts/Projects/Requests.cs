namespace Mastery.Api.Contracts.Projects;

/// <summary>
/// Request to create a new project.
/// </summary>
public sealed record CreateProjectRequest(
    string Title,
    string? Description = null,
    int Priority = 3,
    Guid? GoalId = null,
    Guid? SeasonId = null,
    string? TargetEndDate = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<CreateMilestoneRequest>? Milestones = null,
    bool SaveAsDraft = false);

/// <summary>
/// Request to create a milestone.
/// </summary>
public sealed record CreateMilestoneRequest(
    string Title,
    string? TargetDate = null,
    string? Notes = null);

/// <summary>
/// Request to update a project.
/// </summary>
public sealed record UpdateProjectRequest(
    string? Title = null,
    string? Description = null,
    int? Priority = null,
    Guid? GoalId = null,
    Guid? SeasonId = null,
    string? TargetEndDate = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null);

/// <summary>
/// Request to change project status.
/// </summary>
public sealed record ChangeProjectStatusRequest(
    string NewStatus);

/// <summary>
/// Request to set the next action for a project.
/// </summary>
public sealed record SetProjectNextActionRequest(
    Guid? TaskId);

/// <summary>
/// Request to complete a project.
/// </summary>
public sealed record CompleteProjectRequest(
    string? OutcomeNotes = null);

/// <summary>
/// Request to add a milestone.
/// </summary>
public sealed record AddMilestoneRequest(
    string Title,
    string? TargetDate = null,
    string? Notes = null);

/// <summary>
/// Request to update a milestone.
/// </summary>
public sealed record UpdateMilestoneRequest(
    string? Title = null,
    string? TargetDate = null,
    string? Notes = null,
    int? DisplayOrder = null);
