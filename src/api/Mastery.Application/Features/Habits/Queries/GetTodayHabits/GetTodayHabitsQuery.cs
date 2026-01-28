using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Models;

namespace Mastery.Application.Features.Habits.Queries.GetTodayHabits;

/// <summary>
/// Gets habits for today's daily loop - optimized for quick completion UI.
/// </summary>
public sealed record GetTodayHabitsQuery : IQuery<List<TodayHabitDto>>;
