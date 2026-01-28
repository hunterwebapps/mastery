using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.CompleteTask;

/// <summary>
/// Completes a task.
/// </summary>
public sealed record CompleteTaskCommand(
    Guid TaskId,
    string CompletedOn,
    int? ActualMinutes = null,
    string? Note = null,
    decimal? EnteredValue = null) : ICommand;
