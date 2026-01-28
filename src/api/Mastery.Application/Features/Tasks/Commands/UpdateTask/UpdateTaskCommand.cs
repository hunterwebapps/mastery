using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.UpdateTask;

/// <summary>
/// Updates an existing task's properties.
/// </summary>
public sealed record UpdateTaskCommand(
    Guid TaskId,
    string? Title = null,
    string? Description = null,
    int? EstimatedMinutes = null,
    int? EnergyCost = null,
    int? Priority = null,
    Guid? ProjectId = null,
    Guid? GoalId = null,
    UpdateTaskDueInput? Due = null,
    bool ClearDue = false,
    List<string>? ContextTags = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null) : ICommand;

/// <summary>
/// Input for updating a task due date configuration.
/// </summary>
public sealed record UpdateTaskDueInput(
    string DueOn,
    string? DueAt = null,
    string DueType = "Soft");
