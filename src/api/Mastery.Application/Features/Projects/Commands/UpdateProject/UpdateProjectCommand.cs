using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.UpdateProject;

/// <summary>
/// Updates an existing project's details.
/// </summary>
public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string? Title = null,
    string? Description = null,
    int? Priority = null,
    Guid? GoalId = null,
    Guid? SeasonId = null,
    string? TargetEndDate = null) : ICommand;
