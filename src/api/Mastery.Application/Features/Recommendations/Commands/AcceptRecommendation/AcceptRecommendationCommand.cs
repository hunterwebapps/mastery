using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;

namespace Mastery.Application.Features.Recommendations.Commands.AcceptRecommendation;

public sealed record AcceptRecommendationCommand(Guid RecommendationId) : ICommand<ExecutionResult>;
