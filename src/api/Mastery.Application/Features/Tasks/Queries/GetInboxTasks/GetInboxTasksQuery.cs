using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Tasks.Models;

namespace Mastery.Application.Features.Tasks.Queries.GetInboxTasks;

/// <summary>
/// Gets tasks in Inbox status for capture/triage UI.
/// </summary>
public sealed record GetInboxTasksQuery : IQuery<List<InboxTaskDto>>;
