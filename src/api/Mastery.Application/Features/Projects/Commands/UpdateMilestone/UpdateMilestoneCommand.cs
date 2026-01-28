using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.UpdateMilestone;

/// <summary>
/// Updates an existing milestone.
/// </summary>
public sealed record UpdateMilestoneCommand(
    Guid ProjectId,
    Guid MilestoneId,
    string? Title = null,
    string? TargetDate = null,
    string? Notes = null,
    int? DisplayOrder = null) : ICommand;
