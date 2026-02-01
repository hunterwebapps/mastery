using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Policies;

/// <summary>
/// Prevents duplicate recommendations for the same target entity.
/// Rejects recommendations if a pending one already exists for the same target.
/// </summary>
public sealed class DuplicateRecommendationPolicyRule(
    IRecommendationRepository _recommendationRepository,
    ILogger<DuplicateRecommendationPolicyRule> _logger)
    : IPolicyRule
{
    public string Name => "DuplicateRecommendation";
    public int Order => 50; // Run early to avoid wasted work

    public async Task<PolicyRuleResult> EvaluateAsync(
        IReadOnlyList<Recommendation> recommendations,
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken ct = default)
    {
        var violations = new List<PolicyViolation>();

        foreach (var rec in recommendations.Where(r => r.Target.EntityId.HasValue))
        {
            var exists = await _recommendationRepository.ExistsPendingForTargetAsync(
                state.UserId,
                rec.Type,
                rec.Target.Kind,
                rec.Target.EntityId,
                ct);

            if (exists)
            {
                _logger.LogDebug(
                    "Rejecting duplicate recommendation {RecommendationId} for target {TargetKind}:{TargetId}",
                    rec.Id, rec.Target.Kind, rec.Target.EntityId);

                violations.Add(new PolicyViolation(
                    RuleName: Name,
                    Description: $"Pending recommendation of type {rec.Type} already exists for this target",
                    Severity: PolicyViolationSeverity.Rejected,
                    AffectedRecommendationId: rec.Id));
            }
        }

        return new PolicyRuleResult(violations);
    }
}
