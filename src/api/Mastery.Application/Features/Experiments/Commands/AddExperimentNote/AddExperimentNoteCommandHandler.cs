using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.AddExperimentNote;

public sealed class AddExperimentNoteCommandHandler : ICommandHandler<AddExperimentNoteCommand, Guid>
{
    private readonly IExperimentRepository _experimentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AddExperimentNoteCommandHandler(
        IExperimentRepository experimentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _experimentRepository = experimentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddExperimentNoteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var experiment = await _experimentRepository.GetByIdWithDetailsAsync(request.ExperimentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Experiment.Experiment), request.ExperimentId);

        if (experiment.UserId != userId)
            throw new DomainException("Experiment does not belong to the current user.");

        var note = experiment.AddNote(request.Content);

        await _experimentRepository.UpdateAsync(experiment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return note.Id;
    }
}
