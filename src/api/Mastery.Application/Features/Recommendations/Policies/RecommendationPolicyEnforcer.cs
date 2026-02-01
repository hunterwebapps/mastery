using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Policies;

/// <summary>
/// Enforces hard constraints on recommendations before persistence.
/// Runs a chain of policy rules against recommendations.
/// </summary>
public sealed class RecommendationPolicyEnforcer(
    IEnumerable<IPolicyRule> _policyRules,
    ILogger<RecommendationPolicyEnforcer> _logger)
    : IRecommendationPolicyEnforcer
{
    public async Task<PolicyEnforcementResult> EnforceAsync(
        IReadOnlyList<Recommendation> recommendations,
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken ct = default)
    {
        var approved = new List<Recommendation>(recommendations);
        var rejected = new List<RejectedRecommendation>();
        var violations = new List<PolicyViolation>();

        _logger.LogDebug(
            "Enforcing policies on {Count} recommendations for user {UserId}",
            recommendations.Count, state.UserId);

        // Run each policy rule
        foreach (var rule in _policyRules.OrderBy(r => r.Order))
        {
            if (approved.Count == 0)
                break;

            var ruleResult = await rule.EvaluateAsync(approved, state, context, ct);

            if (ruleResult.Violations.Count > 0)
            {
                violations.AddRange(ruleResult.Violations);

                // Remove rejected recommendations from approved list
                foreach (var violation in ruleResult.Violations
                    .Where(v => v.Severity == PolicyViolationSeverity.Rejected && v.AffectedRecommendationId.HasValue))
                {
                    var toRemove = approved.FirstOrDefault(r => r.Id == violation.AffectedRecommendationId);
                    if (toRemove != null)
                    {
                        approved.Remove(toRemove);
                        rejected.Add(new RejectedRecommendation(toRemove, rule.Name, violation.Description));

                        _logger.LogInformation(
                            "Recommendation {RecommendationId} rejected by policy {PolicyName}: {Reason}",
                            toRemove.Id, rule.Name, violation.Description);
                    }
                }
            }
        }

        _logger.LogInformation(
            "Policy enforcement complete: {Approved} approved, {Rejected} rejected, {Violations} violations",
            approved.Count, rejected.Count, violations.Count);

        return new PolicyEnforcementResult(approved, rejected, violations);
    }
}

/// <summary>
/// Interface for individual policy rules.
/// </summary>
public interface IPolicyRule
{
    /// <summary>
    /// Rule name for logging and auditing.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Order in which this rule is evaluated. Lower runs first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Evaluates the policy rule against recommendations.
    /// </summary>
    Task<PolicyRuleResult> EvaluateAsync(
        IReadOnlyList<Recommendation> recommendations,
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a single policy rule evaluation.
/// </summary>
public sealed record PolicyRuleResult(
    IReadOnlyList<PolicyViolation> Violations)
{
    public static PolicyRuleResult NoViolations => new([]);
}
