using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.ScheduleTask;

/// <summary>
/// Schedules a task for a specific date.
/// </summary>
public sealed record ScheduleTaskCommand(
    Guid TaskId,
    string ScheduledOn,
    string? PreferredTimeWindowStart = null,
    string? PreferredTimeWindowEnd = null) : ICommand;
