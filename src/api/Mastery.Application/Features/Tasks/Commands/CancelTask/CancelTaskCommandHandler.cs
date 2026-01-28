using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.CancelTask;

public sealed class CancelTaskCommandHandler : ICommandHandler<CancelTaskCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CancelTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async System.Threading.Tasks.Task Handle(CancelTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        if (task.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        task.Cancel();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
