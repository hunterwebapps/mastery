using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Models;

namespace Mastery.Application.Features.Habits.Queries.GetHabits;

/// <summary>
/// Gets all habits for the current user, optionally filtered by status.
/// </summary>
public sealed record GetHabitsQuery(string? Status = null) : IQuery<List<HabitSummaryDto>>;
