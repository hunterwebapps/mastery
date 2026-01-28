namespace Mastery.Api.Contracts.Recommendations;

public sealed record GenerateRecommendationsRequest(
    string Context);

public sealed record DismissRecommendationRequest(
    string? Reason = null);
