using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.UndoOccurrence;

/// <summary>
/// Undoes a completed habit occurrence.
/// </summary>
public sealed record UndoOccurrenceCommand(
    Guid HabitId,
    string Date) : ICommand;
