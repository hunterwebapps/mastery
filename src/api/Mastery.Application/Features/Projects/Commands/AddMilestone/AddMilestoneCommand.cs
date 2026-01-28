using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.AddMilestone;

/// <summary>
/// Adds a new milestone to an existing project.
/// </summary>
public sealed record AddMilestoneCommand(
    Guid ProjectId,
    string Title,
    string? TargetDate = null,
    string? Notes = null) : ICommand<Guid>;
