using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.SkipOccurrence;

/// <summary>
/// Skips a habit occurrence.
/// </summary>
public sealed record SkipOccurrenceCommand(
    Guid HabitId,
    string Date,
    string? Reason = null) : ICommand;
