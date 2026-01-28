using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// Creates a new project for the current user.
/// </summary>
public sealed record CreateProjectCommand(
    string Title,
    string? Description = null,
    int Priority = 3,
    Guid? GoalId = null,
    Guid? SeasonId = null,
    string? TargetEndDate = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<CreateMilestoneInput>? Milestones = null,
    bool SaveAsDraft = false) : ICommand<Guid>;

/// <summary>
/// Input for creating a milestone.
/// </summary>
public sealed record CreateMilestoneInput(
    string Title,
    string? TargetDate = null,
    string? Notes = null);
