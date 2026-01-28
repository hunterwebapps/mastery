using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

public interface IRecommendationPipeline
{
    Task<IReadOnlyList<RecommendationSummaryDto>> ExecuteAsync(
        string userId,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}
