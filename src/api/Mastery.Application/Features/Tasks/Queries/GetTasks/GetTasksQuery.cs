using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Tasks.Models;

namespace Mastery.Application.Features.Tasks.Queries.GetTasks;

/// <summary>
/// Gets tasks with optional filtering.
/// </summary>
public sealed record GetTasksQuery(
    string? Status = null,
    Guid? ProjectId = null,
    Guid? GoalId = null,
    string? ContextTag = null,
    bool? IsOverdue = null) : IQuery<List<TaskSummaryDto>>;
