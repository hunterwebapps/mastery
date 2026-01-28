using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Goals.Commands.DeleteGoal;

/// <summary>
/// Archives (soft-deletes) a goal.
/// </summary>
public sealed record DeleteGoalCommand(Guid Id) : ICommand;
