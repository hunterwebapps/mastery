using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;

namespace Mastery.Application.Features.Recommendations.Queries.GetRecommendationHistory;

public sealed record GetRecommendationHistoryQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IQuery<IReadOnlyList<RecommendationSummaryDto>>;
