using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Application.Features.Recommendations.Commands.GenerateRecommendations;

public sealed class GenerateRecommendationsCommandHandler
    : ICommandHandler<GenerateRecommendationsCommand, IReadOnlyList<RecommendationSummaryDto>>
{
    private readonly IRecommendationPipeline _pipeline;
    private readonly ICurrentUserService _currentUserService;

    public GenerateRecommendationsCommandHandler(
        IRecommendationPipeline pipeline,
        ICurrentUserService currentUserService)
    {
        _pipeline = pipeline;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<RecommendationSummaryDto>> Handle(
        GenerateRecommendationsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        if (!Enum.TryParse<RecommendationContext>(request.Context, out var context))
            throw new DomainException($"Invalid recommendation context: {request.Context}");

        return await _pipeline.ExecuteAsync(userId, context, cancellationToken);
    }
}
