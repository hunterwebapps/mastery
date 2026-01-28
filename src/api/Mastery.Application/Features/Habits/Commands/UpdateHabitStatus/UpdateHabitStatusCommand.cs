using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.UpdateHabitStatus;

/// <summary>
/// Updates a habit's status (Activate, Pause, Archive).
/// </summary>
public sealed record UpdateHabitStatusCommand(
    Guid HabitId,
    string Status) : ICommand;
