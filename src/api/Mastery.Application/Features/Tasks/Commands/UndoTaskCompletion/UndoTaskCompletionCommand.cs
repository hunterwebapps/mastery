using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.UndoTaskCompletion;

/// <summary>
/// Undoes a task completion.
/// </summary>
public sealed record UndoTaskCompletionCommand(Guid TaskId) : ICommand;
