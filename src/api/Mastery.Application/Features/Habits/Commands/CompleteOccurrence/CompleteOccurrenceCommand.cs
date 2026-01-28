using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.CompleteOccurrence;

/// <summary>
/// Completes a habit occurrence for a specific date.
/// </summary>
public sealed record CompleteOccurrenceCommand(
    Guid HabitId,
    string Date,
    string? Mode = null,
    decimal? Value = null,
    string? Note = null) : ICommand;
