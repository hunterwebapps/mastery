using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Recommendations.Commands.SnoozeRecommendation;

public sealed record SnoozeRecommendationCommand(Guid RecommendationId) : ICommand;
