using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Tasks.Models;

namespace Mastery.Application.Features.Tasks.Queries.GetTodayTasks;

/// <summary>
/// Gets tasks for today's daily loop - optimized for quick completion UI.
/// </summary>
public sealed record GetTodayTasksQuery : IQuery<List<TodayTaskDto>>;
