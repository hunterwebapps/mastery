using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Tasks.Models;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Tasks.Queries.GetInboxTasks;

public sealed class GetInboxTasksQueryHandler : IQueryHandler<GetInboxTasksQuery, List<InboxTaskDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetInboxTasksQueryHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<InboxTaskDto>> Handle(GetInboxTasksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var tasks = await _taskRepository.GetInboxTasksAsync(userId, cancellationToken);

        return tasks.Select(task => new InboxTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            EstimatedMinutes = task.EstimatedMinutes,
            EnergyCost = task.EnergyCost,
            ContextTags = task.ContextTags.Select(t => t.ToString()).ToList(),
            CreatedAt = task.CreatedAt
        }).ToList();
    }
}
