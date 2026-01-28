using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.MoveTaskToReady;

/// <summary>
/// Moves a task from Inbox to Ready status.
/// </summary>
public sealed record MoveTaskToReadyCommand(Guid TaskId) : ICommand;
