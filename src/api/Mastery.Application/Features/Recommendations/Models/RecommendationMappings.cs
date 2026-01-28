using Mastery.Domain.Entities.Recommendation;

namespace Mastery.Application.Features.Recommendations.Models;

public static class RecommendationMappings
{
    public static RecommendationDto ToDto(this Recommendation rec)
    {
        return new RecommendationDto
        {
            Id = rec.Id,
            UserId = rec.UserId,
            Type = rec.Type.ToString(),
            Status = rec.Status.ToString(),
            Context = rec.Context.ToString(),
            TargetKind = rec.Target.Kind.ToString(),
            TargetEntityId = rec.Target.EntityId,
            TargetEntityTitle = rec.Target.EntityTitle,
            ActionKind = rec.ActionKind.ToString(),
            Title = rec.Title,
            Rationale = rec.Rationale,
            ActionPayload = rec.ActionPayload,
            Score = rec.Score,
            ExpiresAt = rec.ExpiresAt,
            RespondedAt = rec.RespondedAt,
            DismissReason = rec.DismissReason,
            SignalIds = rec.SignalIds.ToList(),
            Trace = rec.Trace?.ToDto(),
            CreatedAt = rec.CreatedAt,
            ModifiedAt = rec.ModifiedAt
        };
    }

    public static RecommendationSummaryDto ToSummaryDto(this Recommendation rec)
    {
        return new RecommendationSummaryDto
        {
            Id = rec.Id,
            Type = rec.Type.ToString(),
            Status = rec.Status.ToString(),
            Context = rec.Context.ToString(),
            TargetKind = rec.Target.Kind.ToString(),
            TargetEntityId = rec.Target.EntityId,
            TargetEntityTitle = rec.Target.EntityTitle,
            ActionKind = rec.ActionKind.ToString(),
            Title = rec.Title,
            Rationale = rec.Rationale,
            Score = rec.Score,
            ExpiresAt = rec.ExpiresAt,
            CreatedAt = rec.CreatedAt
        };
    }

    public static RecommendationTraceDto ToDto(this RecommendationTrace trace)
    {
        return new RecommendationTraceDto
        {
            Id = trace.Id,
            StateSnapshotJson = trace.StateSnapshotJson,
            SignalsSummaryJson = trace.SignalsSummaryJson,
            CandidateListJson = trace.CandidateListJson,
            PromptVersion = trace.PromptVersion,
            ModelVersion = trace.ModelVersion,
            RawLlmResponse = trace.RawLlmResponse,
            SelectionMethod = trace.SelectionMethod
        };
    }

    public static DiagnosticSignalDto ToDto(this DiagnosticSignal signal)
    {
        return new DiagnosticSignalDto
        {
            Id = signal.Id,
            Type = signal.Type.ToString(),
            Title = signal.Title,
            Description = signal.Description,
            Severity = signal.Severity,
            EvidenceMetric = signal.Evidence.Metric,
            EvidenceCurrentValue = signal.Evidence.CurrentValue,
            EvidenceThresholdValue = signal.Evidence.ThresholdValue,
            EvidenceDetail = signal.Evidence.Detail,
            DetectedOn = signal.DetectedOn,
            IsActive = signal.IsActive
        };
    }
}
