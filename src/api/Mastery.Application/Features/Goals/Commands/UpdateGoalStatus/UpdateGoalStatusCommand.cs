using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoalStatus;

/// <summary>
/// Changes the status of a goal (activate, pause, complete, archive).
/// </summary>
public sealed record UpdateGoalStatusCommand(
    Guid Id,
    string NewStatus,
    string? CompletionNotes = null) : ICommand;
