using Mastery.Domain.Common;

namespace Mastery.Domain.Entities.Recommendation;

/// <summary>
/// Audit trail for a recommendation - stores the full pipeline context.
/// Child entity of Recommendation (not an aggregate root).
/// </summary>
public sealed class RecommendationTrace : AuditableEntity
{
    public Guid RecommendationId { get; private set; }
    public string StateSnapshotJson { get; private set; } = null!;
    public string SignalsSummaryJson { get; private set; } = null!;
    public string CandidateListJson { get; private set; } = null!;
    public string? PromptVersion { get; private set; }
    public string? ModelVersion { get; private set; }
    public string? RawLlmResponse { get; private set; }
    public string SelectionMethod { get; private set; } = null!;

    private RecommendationTrace() { } // EF Core

    public static RecommendationTrace Create(
        Guid recommendationId,
        string stateSnapshotJson,
        string signalsSummaryJson,
        string candidateListJson,
        string selectionMethod,
        string? promptVersion = null,
        string? modelVersion = null,
        string? rawLlmResponse = null)
    {
        return new RecommendationTrace
        {
            RecommendationId = recommendationId,
            StateSnapshotJson = stateSnapshotJson,
            SignalsSummaryJson = signalsSummaryJson,
            CandidateListJson = candidateListJson,
            SelectionMethod = selectionMethod,
            PromptVersion = promptVersion,
            ModelVersion = modelVersion,
            RawLlmResponse = rawLlmResponse
        };
    }
}
