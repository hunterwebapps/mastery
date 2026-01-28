using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;

namespace Mastery.Application.Features.Recommendations.Queries.GetActiveRecommendations;

public sealed record GetActiveRecommendationsQuery(
    string? Context = null) : IQuery<IReadOnlyList<RecommendationSummaryDto>>;
