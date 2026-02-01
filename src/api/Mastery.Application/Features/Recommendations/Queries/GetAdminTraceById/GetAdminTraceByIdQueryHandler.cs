using System.Text.Json;
using Mastery.Application.Common;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Queries.GetAdminTraceById;

public sealed class GetAdminTraceByIdQueryHandler(
    IRecommendationRepository _recommendationRepository,
    IUserManagementService _userManagementService)
    : IQueryHandler<GetAdminTraceByIdQuery, AdminTraceDetailDto?>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<AdminTraceDetailDto?> Handle(
        GetAdminTraceByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Get trace with recommendation
        var (trace, rec) = await _recommendationRepository.GetTraceByIdAsync(request.TraceId, cancellationToken);

        if (trace is null || rec is null)
        {
            return null;
        }

        // Get user email
        var user = await _userManagementService.GetUserByIdAsync(rec.UserId, cancellationToken);
        var userEmail = user?.Email ?? rec.UserId;

        // Get agent runs for this trace
        var agentRunEntities = await _recommendationRepository.GetAgentRunsByTraceIdAsync(trace.Id, cancellationToken);
        var agentRuns = agentRunEntities.Select(ar => new AgentRunDto
        {
            Id = ar.Id,
            Stage = ar.Stage,
            Model = ar.Model,
            Provider = ar.Provider,
            InputTokens = ar.InputTokens,
            OutputTokens = ar.OutputTokens,
            CachedInputTokens = ar.CachedInputTokens,
            ReasoningTokens = ar.ReasoningTokens,
            LatencyMs = ar.LatencyMs,
            ErrorType = ar.ErrorType,
            ErrorMessage = ar.ErrorMessage,
            RetryCount = ar.RetryCount,
            SystemFingerprint = ar.SystemFingerprint,
            RequestId = ar.RequestId,
            StartedAt = ar.StartedAt,
            CompletedAt = ar.CompletedAt
        }).ToList();

        // Decompress and parse JSON fields
        object? stateSnapshot = null;
        try
        {
            stateSnapshot = JsonCompressionHelper.DeserializeCompressed<object>(trace.StateSnapshotJson);
        }
        catch
        {
            // If decompression fails, try to parse as regular JSON
            try
            {
                stateSnapshot = JsonSerializer.Deserialize<object>(trace.StateSnapshotJson, JsonOptions);
            }
            catch
            {
                // If all else fails, return the raw string wrapped in an object
                stateSnapshot = new { raw = trace.StateSnapshotJson, note = "Unable to parse" };
            }
        }

        object? signalsSummary = null;
        if (!string.IsNullOrEmpty(trace.SignalsSummaryJson))
        {
            try
            {
                signalsSummary = JsonSerializer.Deserialize<object>(trace.SignalsSummaryJson, JsonOptions);
            }
            catch
            {
                signalsSummary = new { raw = trace.SignalsSummaryJson };
            }
        }

        object? candidateList = null;
        if (!string.IsNullOrEmpty(trace.CandidateListJson))
        {
            try
            {
                candidateList = JsonSerializer.Deserialize<object>(trace.CandidateListJson, JsonOptions);
            }
            catch
            {
                candidateList = new { raw = trace.CandidateListJson };
            }
        }

        object? tier0TriggeredRules = null;
        if (!string.IsNullOrEmpty(trace.Tier0TriggeredRulesJson))
        {
            try
            {
                tier0TriggeredRules = JsonSerializer.Deserialize<object>(trace.Tier0TriggeredRulesJson, JsonOptions);
            }
            catch
            {
                tier0TriggeredRules = new { raw = trace.Tier0TriggeredRulesJson };
            }
        }

        object? tier1Scores = null;
        if (!string.IsNullOrEmpty(trace.Tier1ScoresJson))
        {
            try
            {
                tier1Scores = JsonSerializer.Deserialize<object>(trace.Tier1ScoresJson, JsonOptions);
            }
            catch
            {
                tier1Scores = new { raw = trace.Tier1ScoresJson };
            }
        }

        object? policyResult = null;
        if (!string.IsNullOrEmpty(trace.PolicyResultJson))
        {
            try
            {
                policyResult = JsonSerializer.Deserialize<object>(trace.PolicyResultJson, JsonOptions);
            }
            catch
            {
                policyResult = new { raw = trace.PolicyResultJson };
            }
        }

        return new AdminTraceDetailDto
        {
            Id = trace.Id,
            RecommendationId = trace.RecommendationId,
            UserId = rec.UserId,
            UserEmail = userEmail,
            RecommendationType = rec.Type.ToString(),
            RecommendationStatus = rec.Status.ToString(),
            RecommendationTitle = rec.Title,
            RecommendationRationale = rec.Rationale,
            Context = rec.Context.ToString(),
            RecommendationScore = rec.Score,
            SelectionMethod = trace.SelectionMethod,
            PromptVersion = trace.PromptVersion,
            ModelVersion = trace.ModelVersion,
            FinalTier = trace.FinalTier,
            ProcessingWindowType = trace.ProcessingWindowType,
            TotalDurationMs = trace.TotalDurationMs,
            StateSnapshot = stateSnapshot,
            SignalsSummary = signalsSummary,
            CandidateList = candidateList,
            Tier0TriggeredRules = tier0TriggeredRules,
            Tier1Scores = tier1Scores,
            Tier1EscalationReason = trace.Tier1EscalationReason,
            PolicyResult = policyResult,
            RawLlmResponse = trace.RawLlmResponse,
            AgentRuns = agentRuns,
            CreatedAt = trace.CreatedAt,
            ModifiedAt = trace.ModifiedAt
        };
    }
}
