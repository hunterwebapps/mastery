using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.ResumeExperiment;

public sealed class ResumeExperimentCommandHandler : ICommandHandler<ResumeExperimentCommand>
{
    private readonly IExperimentRepository _experimentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ResumeExperimentCommandHandler(
        IExperimentRepository experimentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _experimentRepository = experimentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ResumeExperimentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var experiment = await _experimentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Experiment.Experiment), request.Id);

        if (experiment.UserId != userId)
            throw new DomainException("Experiment does not belong to the current user.");

        // Ensure no other experiment is currently active for this user
        var hasActive = await _experimentRepository.HasActiveExperimentAsync(userId, cancellationToken);
        if (hasActive)
            throw new DomainException("You already have an active experiment. Complete or abandon it before resuming another.");

        experiment.Resume();

        await _experimentRepository.UpdateAsync(experiment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
