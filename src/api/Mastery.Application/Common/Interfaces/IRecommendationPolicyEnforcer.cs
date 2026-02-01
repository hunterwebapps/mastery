using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Enforces hard constraints on recommendations before persistence.
/// Ensures LLM-selected recommendations are still feasible given constraints.
/// </summary>
public interface IRecommendationPolicyEnforcer
{
    /// <summary>
    /// Validates recommendations against policy rules and filters/adjusts as needed.
    /// </summary>
    Task<PolicyEnforcementResult> EnforceAsync(
        IReadOnlyList<Recommendation> recommendations,
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken ct = default);
}

/// <summary>
/// Result of policy enforcement on recommendations.
/// </summary>
public sealed record PolicyEnforcementResult(
    IReadOnlyList<Recommendation> ApprovedRecommendations,
    IReadOnlyList<RejectedRecommendation> RejectedRecommendations,
    IReadOnlyList<PolicyViolation> Violations)
{
    /// <summary>
    /// Returns true if any recommendations were rejected or modified.
    /// </summary>
    public bool HadAdjustments => RejectedRecommendations.Count > 0 || Violations.Count > 0;
}

/// <summary>
/// A recommendation that was rejected by policy enforcement.
/// </summary>
public sealed record RejectedRecommendation(
    Recommendation Recommendation,
    string RuleViolated,
    string Reason);

/// <summary>
/// A policy violation that was detected.
/// </summary>
public sealed record PolicyViolation(
    string RuleName,
    string Description,
    PolicyViolationSeverity Severity,
    Guid? AffectedRecommendationId);

/// <summary>
/// Severity of a policy violation.
/// </summary>
public enum PolicyViolationSeverity
{
    /// <summary>
    /// Warning only - recommendation still allowed.
    /// </summary>
    Warning,

    /// <summary>
    /// Recommendation was rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Recommendation was modified to comply.
    /// </summary>
    Modified,
}
