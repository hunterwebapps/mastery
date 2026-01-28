using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Models;

namespace Mastery.Application.Features.Habits.Queries.GetHabitHistory;

/// <summary>
/// Gets habit occurrence history within a date range.
/// </summary>
public sealed record GetHabitHistoryQuery(
    Guid HabitId,
    string FromDate,
    string ToDate) : IQuery<HabitHistoryDto>;
