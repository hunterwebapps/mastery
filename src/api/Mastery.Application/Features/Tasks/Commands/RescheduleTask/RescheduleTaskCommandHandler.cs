using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.RescheduleTask;

public sealed class RescheduleTaskCommandHandler : ICommandHandler<RescheduleTaskCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async System.Threading.Tasks.Task Handle(RescheduleTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        if (task.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        if (!DateOnly.TryParse(request.NewDate, out var newDate))
            throw new DomainException($"Invalid date: {request.NewDate}");

        RescheduleReason? reason = null;
        if (!string.IsNullOrEmpty(request.Reason) && Enum.TryParse<RescheduleReason>(request.Reason, out var r))
            reason = r;

        task.Reschedule(newDate, reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
