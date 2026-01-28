using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Recommendations.Commands.AcceptRecommendation;

public sealed record AcceptRecommendationCommand(Guid RecommendationId) : ICommand;
