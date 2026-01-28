using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Recommendations.Commands.DismissRecommendation;

public sealed record DismissRecommendationCommand(
    Guid RecommendationId,
    string? Reason = null) : ICommand;
