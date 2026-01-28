using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Goals.Models;

namespace Mastery.Application.Features.Goals.Queries.GetGoals;

/// <summary>
/// Gets all goals for the current user, optionally filtered by status.
/// </summary>
public sealed record GetGoalsQuery(string? Status = null) : IQuery<IReadOnlyList<GoalSummaryDto>>;
