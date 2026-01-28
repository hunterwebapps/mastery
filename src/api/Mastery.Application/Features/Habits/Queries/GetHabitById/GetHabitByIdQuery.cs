using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Models;

namespace Mastery.Application.Features.Habits.Queries.GetHabitById;

/// <summary>
/// Gets a habit by ID with full details.
/// </summary>
public sealed record GetHabitByIdQuery(Guid HabitId) : IQuery<HabitDto>;
