using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.CompleteTask;

public sealed class CompleteTaskCommandHandler : ICommandHandler<CompleteTaskCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async System.Threading.Tasks.Task Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var task = await _taskRepository.GetByIdWithDetailsAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        if (task.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        if (!DateOnly.TryParse(request.CompletedOn, out var completedOn))
            throw new DomainException($"Invalid completion date: {request.CompletedOn}");

        task.Complete(completedOn, request.ActualMinutes, request.Note, request.EnteredValue);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
