using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.CompleteMilestone;

/// <summary>
/// Marks a milestone as completed.
/// </summary>
public sealed record CompleteMilestoneCommand(
    Guid ProjectId,
    Guid MilestoneId) : ICommand;
