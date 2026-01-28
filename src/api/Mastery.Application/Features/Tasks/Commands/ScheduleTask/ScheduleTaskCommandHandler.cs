using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Tasks.Commands.ScheduleTask;

public sealed class ScheduleTaskCommandHandler : ICommandHandler<ScheduleTaskCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ScheduleTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async System.Threading.Tasks.Task Handle(ScheduleTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        if (task.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        if (!DateOnly.TryParse(request.ScheduledOn, out var scheduledOn))
            throw new DomainException($"Invalid scheduled date: {request.ScheduledOn}");

        TimeWindow? timeWindow = null;
        if (!string.IsNullOrEmpty(request.PreferredTimeWindowStart) &&
            !string.IsNullOrEmpty(request.PreferredTimeWindowEnd))
        {
            if (TimeOnly.TryParse(request.PreferredTimeWindowStart, out var start) &&
                TimeOnly.TryParse(request.PreferredTimeWindowEnd, out var end))
            {
                timeWindow = TimeWindow.Create(start, end);
            }
        }

        task.Schedule(scheduledOn, timeWindow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
