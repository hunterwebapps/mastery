using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.AbandonExperiment;

public sealed class AbandonExperimentCommandHandler : ICommandHandler<AbandonExperimentCommand>
{
    private readonly IExperimentRepository _experimentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AbandonExperimentCommandHandler(
        IExperimentRepository experimentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _experimentRepository = experimentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AbandonExperimentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var experiment = await _experimentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Experiment.Experiment), request.Id);

        if (experiment.UserId != userId)
            throw new DomainException("Experiment does not belong to the current user.");

        experiment.Abandon(request.Reason);

        await _experimentRepository.UpdateAsync(experiment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
