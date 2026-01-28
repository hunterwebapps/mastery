using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Goals.Models;

namespace Mastery.Application.Features.Goals.Queries.GetGoalById;

/// <summary>
/// Gets a single goal by ID with all metrics.
/// </summary>
public sealed record GetGoalByIdQuery(Guid Id) : IQuery<GoalDto>;
