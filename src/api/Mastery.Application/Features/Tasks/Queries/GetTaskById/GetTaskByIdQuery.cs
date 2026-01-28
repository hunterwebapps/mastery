using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Tasks.Models;

namespace Mastery.Application.Features.Tasks.Queries.GetTaskById;

/// <summary>
/// Gets a task by ID with full details.
/// </summary>
public sealed record GetTaskByIdQuery(Guid Id) : IQuery<TaskDto>;
