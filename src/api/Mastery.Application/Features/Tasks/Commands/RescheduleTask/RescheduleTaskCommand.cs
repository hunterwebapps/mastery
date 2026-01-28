using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.RescheduleTask;

/// <summary>
/// Reschedules a task to a new date with optional reason.
/// </summary>
public sealed record RescheduleTaskCommand(
    Guid TaskId,
    string NewDate,
    string? Reason = null) : ICommand;
