using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.CancelTask;

/// <summary>
/// Cancels a task.
/// </summary>
public sealed record CancelTaskCommand(Guid TaskId) : ICommand;
