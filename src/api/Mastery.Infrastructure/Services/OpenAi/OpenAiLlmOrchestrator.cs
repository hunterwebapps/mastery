using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Mastery.Infrastructure.Services.OpenAi.Prompts;
using Mastery.Infrastructure.Services.OpenAi.RAG;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Mastery.Infrastructure.Services.OpenAi;

/// <summary>
/// Two-stage AI pipeline orchestrator.
/// Stage 1: Situational Assessment → Stage 2: Candidate Selection.
/// The LLM selects from pre-computed Tier 0 candidates and provides personalized rationale.
/// </summary>
internal sealed class OpenAiLlmOrchestrator(
    IOptions<OpenAiOptions> _options,
    IRagContextRetriever _ragRetriever,
    ILogger<OpenAiLlmOrchestrator> _logger)
    : IRecommendationOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly string ModelVersion = $"{AssessmentPrompt.Model}|{SelectionPrompt.Model}";

    public async Task<RecommendationOrchestrationResult> OrchestrateAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        IReadOnlyList<DirectRecommendationCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Enabled || string.IsNullOrWhiteSpace(_options.Value.ApiKey))
        {
            _logger.LogWarning("OpenAI disabled or no API key configured — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Disabled");
        }

        if (candidates.Count == 0)
        {
            _logger.LogInformation("No candidates provided — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "No-Candidates");
        }

        try
        {
            return await RunPipelineAsync(state, context, candidates, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI pipeline failed — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Error");
        }
    }

    private async Task<RecommendationOrchestrationResult> RunPipelineAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        IReadOnlyList<DirectRecommendationCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var trace = new PipelineTrace { Candidates = candidates.ToList() };
        var llmCalls = new List<LlmCallRecord>();

        // ── RAG for Stage 1 ────────────────────────────────────────────
        _logger.LogDebug("Retrieving RAG context for assessment stage");
        var assessmentRag = await _ragRetriever.RetrieveForAssessmentAsync(state, context, cancellationToken);
        trace.AssessmentRag = assessmentRag;

        // ── Stage 1: Situational Assessment ────────────────────────────
        _logger.LogInformation("Stage 1: Running situational assessment for context {Context}", context);

        var stage1Result = await RunStage1AssessmentAsync(state, context, assessmentRag, cancellationToken);
        llmCalls.Add(stage1Result.CallRecord);

        if (stage1Result.Assessment is null)
        {
            _logger.LogWarning("Stage 1 failed — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Stage1-Failed",
                LlmCalls: llmCalls);
        }
        trace.Assessment = stage1Result.Assessment;

        // ── RAG for Stage 2 ────────────────────────────────────────────
        _logger.LogDebug("Retrieving RAG context for selection stage");
        var selectionRag = await _ragRetriever.RetrieveForSelectionAsync(stage1Result.Assessment, context, state.UserId, cancellationToken);
        trace.SelectionRag = selectionRag;

        // ── Stage 2: Candidate Selection ───────────────────────────────
        _logger.LogInformation("Stage 2: Selecting from {CandidateCount} candidates", candidates.Count);

        var stage2Result = await RunStage2SelectionAsync(
            stage1Result.Assessment, candidates, context, state.Profile, selectionRag, state.Today, cancellationToken);
        llmCalls.Add(stage2Result.CallRecord);

        if (stage2Result.Selection is null || stage2Result.Selection.Selections.Count == 0)
        {
            _logger.LogWarning("Stage 2 failed or returned no selections — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Stage2-Failed",
                LlmCalls: llmCalls);
        }
        trace.SelectionResult = stage2Result.Selection;

        // ── Convert selections to RecommendationCandidates ─────────────
        var selected = ConvertSelections(candidates, stage2Result.Selection);
        trace.Selected = selected;

        _logger.LogInformation(
            "Pipeline complete: {SelectedCount}/{CandidateCount} candidates selected",
            selected.Count,
            candidates.Count);

        var rawResponse = JsonSerializer.Serialize(trace, JsonOptions);

        return new RecommendationOrchestrationResult(
            SelectedCandidates: selected,
            SelectionMethod: "LLM-Selection-v1",
            PromptVersion: $"{AssessmentPrompt.PromptVersion}|{SelectionPrompt.PromptVersion}",
            ModelVersion: ModelVersion,
            RawResponse: rawResponse,
            LlmCalls: llmCalls);
    }

    // ─────────────────────────────────────────────────────────────────
    // Stage 1: Situational Assessment
    // ─────────────────────────────────────────────────────────────────

    private record Stage1Result(SituationalAssessment? Assessment, LlmCallRecord CallRecord);

    private async Task<Stage1Result> RunStage1AssessmentAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        RagContext? ragContext,
        CancellationToken cancellationToken)
    {
        var result = await CallOpenAiAsync(
            "Assessment",
            AssessmentPrompt.Model,
            AssessmentPrompt.BuildSystemPrompt(context),
            AssessmentPrompt.BuildUserPrompt(state, context, ragContext),
            AssessmentPrompt.SchemaName,
            AssessmentPrompt.ResponseSchema,
            cancellationToken);

        if (result.Content is null)
        {
            return new Stage1Result(null, result.CallRecord);
        }

        try
        {
            var assessment = JsonSerializer.Deserialize<SituationalAssessment>(result.Content, JsonOptions);
            return new Stage1Result(assessment, result.CallRecord);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Stage 1 assessment response");
            return new Stage1Result(null, result.CallRecord);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Stage 2: Candidate Selection
    // ─────────────────────────────────────────────────────────────────

    private record Stage2Result(CandidateSelectionResult? Selection, LlmCallRecord CallRecord);

    private async Task<Stage2Result> RunStage2SelectionAsync(
        SituationalAssessment assessment,
        IReadOnlyList<DirectRecommendationCandidate> candidates,
        RecommendationContext context,
        UserProfileSnapshot profile,
        RagContext? ragContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var result = await CallOpenAiAsync(
            "Selection",
            SelectionPrompt.Model,
            SelectionPrompt.BuildSystemPrompt(context),
            SelectionPrompt.BuildUserPrompt(assessment, candidates, context, profile, ragContext, today),
            SelectionPrompt.SchemaName,
            SelectionPrompt.ResponseSchema,
            cancellationToken);

        if (result.Content is null)
        {
            return new Stage2Result(null, result.CallRecord);
        }

        try
        {
            var selection = JsonSerializer.Deserialize<CandidateSelectionResult>(result.Content, JsonOptions);
            return new Stage2Result(selection, result.CallRecord);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Stage 2 selection response");
            return new Stage2Result(null, result.CallRecord);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Convert selections to RecommendationCandidates
    // ─────────────────────────────────────────────────────────────────

    private List<RecommendationCandidate> ConvertSelections(
        IReadOnlyList<DirectRecommendationCandidate> candidates,
        CandidateSelectionResult selectionResult)
    {
        var result = new List<RecommendationCandidate>();

        // Sort by priority rank
        var orderedSelections = selectionResult.Selections
            .Where(s => s.CandidateIndex >= 0 && s.CandidateIndex < candidates.Count)
            .OrderBy(s => s.PriorityRank)
            .ToList();

        foreach (var selection in orderedSelections)
        {
            var candidate = candidates[selection.CandidateIndex];

            // Build target from DirectRecommendationCandidate
            var target = RecommendationTarget.Create(
                candidate.TargetKind,
                candidate.TargetEntityId,
                candidate.TargetEntityTitle);

            // Use LLM-provided rationale (personalized) instead of generic rule rationale
            var rationale = !string.IsNullOrWhiteSpace(selection.Rationale)
                ? selection.Rationale
                : candidate.Rationale;

            // Use refined action summary if provided, otherwise use original
            var actionSummary = !string.IsNullOrWhiteSpace(selection.RefinedActionSummary)
                ? selection.RefinedActionSummary
                : candidate.ActionSummary;

            // Adjust score based on priority rank (higher rank = higher effective score)
            // This ensures the LLM's prioritization is reflected in the final ordering
            var maxRank = orderedSelections.Max(s => s.PriorityRank);
            var rankBoost = maxRank > 1
                ? (maxRank - selection.PriorityRank + 1) * 0.01m
                : 0m;
            var adjustedScore = Math.Min(1.0m, candidate.Score + rankBoost);

            result.Add(new RecommendationCandidate(
                Type: candidate.Type,
                Target: target,
                ActionKind: candidate.ActionKind,
                Title: candidate.Title,
                Rationale: rationale,
                Score: adjustedScore,
                ActionPayload: candidate.ActionPayload,
                ActionSummary: actionSummary));
        }

        // Log any invalid selections
        var invalidSelections = selectionResult.Selections
            .Where(s => s.CandidateIndex < 0 || s.CandidateIndex >= candidates.Count)
            .ToList();

        if (invalidSelections.Count > 0)
        {
            _logger.LogWarning(
                "LLM returned {InvalidCount} invalid candidate indices: {Indices}",
                invalidSelections.Count,
                string.Join(", ", invalidSelections.Select(s => s.CandidateIndex)));
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────
    // OpenAI call helper
    // ─────────────────────────────────────────────────────────────────

    private record OpenAiCallResult(string? Content, LlmCallRecord CallRecord);

    private async Task<OpenAiCallResult> CallOpenAiAsync(
        string stage,
        string model,
        string systemPrompt,
        string userPrompt,
        string schemaName,
        BinaryData responseSchema,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = new ChatClient(model, _options.Value.ApiKey);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
#pragma warning disable OPENAI001
                ReasoningEffortLevel = ChatReasoningEffortLevel.Medium,
#pragma warning restore OPENAI001
                MaxOutputTokenCount = _options.Value.MaxOutputTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: schemaName,
                    jsonSchema: responseSchema,
                    jsonSchemaIsStrict: true)
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.Value.TimeoutSeconds));

            var completion = await client.CompleteChatAsync(messages, options, cts.Token);
            stopwatch.Stop();
            var completedAt = DateTime.UtcNow;

            // Extract usage data
            var usage = completion.Value.Usage;
            var inputTokens = usage?.InputTokenCount ?? 0;
            var outputTokens = usage?.OutputTokenCount ?? 0;
            var cachedInputTokens = usage?.InputTokenDetails?.CachedTokenCount ?? 0;
            var reasoningTokens = usage?.OutputTokenDetails?.ReasoningTokenCount ?? 0;

            var callRecord = new LlmCallRecord(
                Stage: stage,
                Model: model,
                InputTokens: inputTokens,
                OutputTokens: outputTokens,
                LatencyMs: (int)stopwatch.ElapsedMilliseconds,
                StartedAt: startedAt,
                CompletedAt: completedAt,
                CachedInputTokens: cachedInputTokens,
                ReasoningTokens: reasoningTokens,
                SystemFingerprint: completion.Value.SystemFingerprint ?? "",
                RequestId: completion.Value.Id ?? "",
                Provider: "OpenAI");

            var content = completion.Value.Content
                .Where(p => p.Kind == ChatMessageContentPartKind.Text)
                .Select(p => p.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("OpenAI returned empty content. FinishReason={FinishReason}",
                    completion.Value.FinishReason);
                return new OpenAiCallResult(null, callRecord);
            }

            _logger.LogDebug(
                "OpenAI {Stage} response: {InputTokens} in / {OutputTokens} out / {CachedTokens} cached / {ReasoningTokens} reasoning / {LatencyMs}ms",
                stage,
                inputTokens,
                outputTokens,
                cachedInputTokens,
                reasoningTokens,
                stopwatch.ElapsedMilliseconds);

            return new OpenAiCallResult(content, callRecord);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var completedAt = DateTime.UtcNow;

            var errorRecord = new LlmCallRecord(
                Stage: stage,
                Model: model,
                InputTokens: 0,
                OutputTokens: 0,
                LatencyMs: (int)stopwatch.ElapsedMilliseconds,
                StartedAt: startedAt,
                CompletedAt: completedAt,
                CachedInputTokens: 0,
                ReasoningTokens: 0,
                SystemFingerprint: "",
                RequestId: "",
                Provider: "OpenAI",
                ErrorType: ex.GetType().Name,
                ErrorMessage: ex.Message);

            _logger.LogWarning(ex, "OpenAI {Stage} call failed", stage);
            return new OpenAiCallResult(null, errorRecord);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Trace model (stored in RawResponse for explainability)
    // ─────────────────────────────────────────────────────────────────

    private sealed class PipelineTrace
    {
        [JsonPropertyName("candidates")]
        public List<DirectRecommendationCandidate>? Candidates { get; set; }

        [JsonPropertyName("assessment")]
        public SituationalAssessment? Assessment { get; set; }

        [JsonPropertyName("selectionResult")]
        public CandidateSelectionResult? SelectionResult { get; set; }

        [JsonPropertyName("selected")]
        public List<RecommendationCandidate>? Selected { get; set; }

        [JsonPropertyName("assessmentRag")]
        public RagContext? AssessmentRag { get; set; }

        [JsonPropertyName("selectionRag")]
        public RagContext? SelectionRag { get; set; }
    }
}
