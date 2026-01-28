using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;

namespace Mastery.Application.Features.Recommendations.Commands.GenerateRecommendations;

public sealed record GenerateRecommendationsCommand(
    string Context) : ICommand<IReadOnlyList<RecommendationSummaryDto>>;
