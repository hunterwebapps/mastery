using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;

namespace Mastery.Application.Features.Recommendations.Queries.GetRecommendationById;

public sealed record GetRecommendationByIdQuery(Guid Id) : IQuery<RecommendationDto>;
