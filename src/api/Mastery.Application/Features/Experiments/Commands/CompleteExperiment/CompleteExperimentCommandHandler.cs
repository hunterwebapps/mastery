using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.CompleteExperiment;

public sealed class CompleteExperimentCommandHandler : ICommandHandler<CompleteExperimentCommand>
{
    private readonly IExperimentRepository _experimentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteExperimentCommandHandler(
        IExperimentRepository experimentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _experimentRepository = experimentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CompleteExperimentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var experiment = await _experimentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Experiment), request.Id);

        if (experiment.UserId != userId)
            throw new DomainException("Experiment does not belong to the current user.");

        // Parse outcome enum
        if (!Enum.TryParse<ExperimentOutcome>(request.OutcomeClassification, out var outcome))
            throw new DomainException($"Invalid outcome classification: {request.OutcomeClassification}");

        // Compute delta and deltaPercent if both baseline and run values are provided
        decimal? delta = null;
        decimal? deltaPercent = null;
        if (request.BaselineValue.HasValue && request.RunValue.HasValue)
        {
            delta = request.RunValue.Value - request.BaselineValue.Value;
            if (request.BaselineValue.Value != 0)
                deltaPercent = (delta.Value / request.BaselineValue.Value) * 100;
        }

        var result = ExperimentResult.Create(
            experimentId: experiment.Id,
            outcomeClassification: outcome,
            baselineValue: request.BaselineValue,
            runValue: request.RunValue,
            delta: delta,
            deltaPercent: deltaPercent,
            complianceRate: request.ComplianceRate,
            narrativeSummary: request.NarrativeSummary);

        experiment.Complete(result);

        await _experimentRepository.UpdateAsync(experiment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
