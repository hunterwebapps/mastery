using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoal;

/// <summary>
/// Updates basic goal properties (title, description, why, priority, deadline, associations).
/// </summary>
public sealed record UpdateGoalCommand(
    Guid Id,
    string Title,
    string? Description = null,
    string? Why = null,
    int Priority = 3,
    DateOnly? Deadline = null,
    Guid? SeasonId = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<Guid>? DependencyIds = null) : ICommand;
