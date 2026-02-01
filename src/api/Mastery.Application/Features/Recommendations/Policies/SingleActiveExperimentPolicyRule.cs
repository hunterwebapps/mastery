using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Policies;

/// <summary>
/// Enforces the single active experiment constraint.
/// Rejects new experiment recommendations if an active experiment already exists.
/// </summary>
public sealed class SingleActiveExperimentPolicyRule(
    ILogger<SingleActiveExperimentPolicyRule> _logger)
    : IPolicyRule
{
    public string Name => "SingleActiveExperiment";
    public int Order => 100;

    public Task<PolicyRuleResult> EvaluateAsync(
        IReadOnlyList<Recommendation> recommendations,
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken ct = default)
    {
        var violations = new List<PolicyViolation>();

        // Check if there's already an active experiment
        var hasActiveExperiment = state.Experiments.Any(e =>
            e.Status == ExperimentStatus.Active || e.Status == ExperimentStatus.Draft);

        if (!hasActiveExperiment)
            return Task.FromResult(PolicyRuleResult.NoViolations);

        // Find experiment recommendations that would violate the constraint
        var experimentRecs = recommendations
            .Where(r => r.Type == RecommendationType.ExperimentRecommendation &&
                        r.ActionKind == RecommendationActionKind.Create)
            .ToList();

        foreach (var rec in experimentRecs)
        {
            _logger.LogDebug(
                "Rejecting experiment recommendation {RecommendationId}: user already has active experiment",
                rec.Id);

            violations.Add(new PolicyViolation(
                RuleName: Name,
                Description: "Cannot create new experiment while another is active",
                Severity: PolicyViolationSeverity.Rejected,
                AffectedRecommendationId: rec.Id));
        }

        return Task.FromResult(new PolicyRuleResult(violations));
    }
}
