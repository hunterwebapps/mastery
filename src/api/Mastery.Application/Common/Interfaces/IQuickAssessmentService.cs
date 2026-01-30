using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Tier 1 quick assessment service that determines whether to escalate to Tier 2 (full LLM pipeline).
/// Uses state delta calculation and vector search to make efficient decisions.
/// </summary>
public interface IQuickAssessmentService
{
    /// <summary>
    /// Performs a quick assessment to determine if Tier 2 processing is needed.
    /// </summary>
    /// <param name="state">The user's current state snapshot.</param>
    /// <param name="signals">The signals being processed.</param>
    /// <param name="tier0Result">The result from Tier 0 deterministic rules.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assessment result with escalation decision.</returns>
    Task<QuickAssessmentResult> AssessAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        RuleEvaluationResult tier0Result,
        CancellationToken ct = default);
}
