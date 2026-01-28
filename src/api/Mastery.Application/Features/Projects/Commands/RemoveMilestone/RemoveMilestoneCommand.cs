using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.RemoveMilestone;

/// <summary>
/// Removes a milestone from a project.
/// </summary>
public sealed record RemoveMilestoneCommand(
    Guid ProjectId,
    Guid MilestoneId) : ICommand;
