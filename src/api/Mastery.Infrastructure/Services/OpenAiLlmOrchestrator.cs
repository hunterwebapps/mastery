using System.Text.Json;
using System.Text.Json.Serialization;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Application.Features.Recommendations.Services;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Services.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Multi-stage AI pipeline orchestrator.
/// Stage 1: Situational Assessment → Stage 2: Strategy → Stage 3: Parallel Domain Generation.
/// Returns empty results on any failure.
/// </summary>
internal sealed class OpenAiLlmOrchestrator(
    IOptions<OpenAiOptions> _options,
    LlmResponseParser _parser,
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

    public async Task<RecommendationOrchestrationResult> OrchestrateAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Enabled || string.IsNullOrWhiteSpace(_options.Value.ApiKey))
        {
            _logger.LogWarning("OpenAI disabled or no API key configured — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Disabled");
        }

        try
        {
            return await RunPipelineAsync(state, context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI pipeline failed — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Error");
        }
    }

    private static readonly string ModelVersion = string.Join("|",
        new[] {
            AssessmentPrompt.Model, StrategyPrompt.Model,
            TaskGenerationPrompt.Model, HabitGenerationPrompt.Model,
            ExperimentGenerationPrompt.Model, GoalMetricGenerationPrompt.Model,
            ProjectGenerationPrompt.Model
        }.Distinct());

    private async Task<RecommendationOrchestrationResult> RunPipelineAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken cancellationToken)
    {
        var trace = new PipelineTrace();

        // ── Stage 1: Situational Assessment ────────────────────────────
        _logger.LogInformation("Stage 1: Running situational assessment for context {Context}", context);

        var assessment = await RunStage1AssessmentAsync(state, context, cancellationToken);
        if (assessment is null)
        {
            _logger.LogWarning("Stage 1 failed — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Stage1-Failed");
        }
        trace.Assessment = assessment;

        // ── Stage 2: Recommendation Strategy ───────────────────────────
        _logger.LogInformation("Stage 2: Building recommendation strategy");

        var strategy = await RunStage2StrategyAsync(assessment, context, state.Profile, cancellationToken);
        if (strategy is null)
        {
            _logger.LogWarning("Stage 2 failed — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Stage2-Failed");
        }
        trace.Strategy = strategy;

        // ── Stage 3: Parallel Domain Generation ────────────────────────
        _logger.LogInformation("Stage 3: Generating recommendations across domains (parallel)");

        var generated = await RunStage3GenerationAsync(
            assessment, strategy, state, cancellationToken);
        trace.Generated = generated;

        // Enforce max budget from strategy
        if (generated.Count > strategy.MaxRecommendations)
        {
            generated = generated
                .OrderByDescending(c => c.Score)
                .Take(strategy.MaxRecommendations)
                .ToList();
        }

        _logger.LogInformation("Pipeline complete: {GeneratedCount} recommendations", generated.Count);

        var rawResponse = JsonSerializer.Serialize(trace, JsonOptions);

        return new RecommendationOrchestrationResult(
            SelectedCandidates: generated,
            SelectionMethod: "LLM-Pipeline-v2",
            PromptVersion: $"{AssessmentPrompt.PromptVersion}|{StrategyPrompt.PromptVersion}",
            ModelVersion: ModelVersion,
            RawResponse: rawResponse);
    }

    // ─────────────────────────────────────────────────────────────────
    // Stage 1
    // ─────────────────────────────────────────────────────────────────

    private async Task<SituationalAssessment?> RunStage1AssessmentAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            AssessmentPrompt.Model,
            AssessmentPrompt.BuildSystemPrompt(context),
            AssessmentPrompt.BuildUserPrompt(state, context),
            AssessmentPrompt.SchemaName,
            AssessmentPrompt.ResponseSchema,
            cancellationToken);
        if (json is null) return null;

        try
        {
            return JsonSerializer.Deserialize<SituationalAssessment>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Stage 1 assessment response");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Stage 2
    // ─────────────────────────────────────────────────────────────────

    private async Task<RecommendationStrategy?> RunStage2StrategyAsync(
        SituationalAssessment assessment,
        RecommendationContext context,
        UserProfileSnapshot? profile,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            StrategyPrompt.Model,
            StrategyPrompt.BuildSystemPrompt(context),
            StrategyPrompt.BuildUserPrompt(assessment, context, profile),
            StrategyPrompt.SchemaName,
            StrategyPrompt.ResponseSchema,
            cancellationToken);
        if (json is null) return null;

        try
        {
            return JsonSerializer.Deserialize<RecommendationStrategy>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Stage 2 strategy response");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Stage 3 — parallel domain generation
    // ─────────────────────────────────────────────────────────────────

    private async Task<List<RecommendationCandidate>> RunStage3GenerationAsync(
        SituationalAssessment assessment,
        RecommendationStrategy strategy,
        UserStateSnapshot state,
        CancellationToken cancellationToken)
    {
        var plan = strategy.InterventionPlan;
        var all = new List<RecommendationCandidate>();

        // Group intervention plan items by domain
        var taskTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "NextBestAction", "TaskBreakdownSuggestion", "ScheduleAdjustmentSuggestion", "PlanRealismAdjustment", "TaskEditSuggestion", "TaskArchiveSuggestion" };
        var habitTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "HabitModeSuggestion", "HabitFromLeadMetricSuggestion", "HabitEditSuggestion", "HabitArchiveSuggestion" };
        var experimentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "ExperimentRecommendation", "CheckInConsistencyNudge", "ExperimentEditSuggestion", "ExperimentArchiveSuggestion" };
        var goalMetricTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "GoalScoreboardSuggestion", "MetricObservationReminder", "GoalEditSuggestion", "GoalArchiveSuggestion", "MetricEditSuggestion" };
        var projectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "ProjectStuckFix", "ProjectSuggestion", "ProjectEditSuggestion", "ProjectArchiveSuggestion" };

        var taskItems = plan.Where(i => taskTypes.Contains(i.TargetType)).ToList();
        var habitItems = plan.Where(i => habitTypes.Contains(i.TargetType)).ToList();
        var experimentItems = plan.Where(i => experimentTypes.Contains(i.TargetType)).ToList();
        var goalMetricItems = plan.Where(i => goalMetricTypes.Contains(i.TargetType)).ToList();
        var projectItems = plan.Where(i => projectTypes.Contains(i.TargetType)).ToList();

        // Launch domain prompts in parallel
        var tasks = new List<Task<List<RecommendationCandidate>>>();

        if (taskItems.Count > 0)
            tasks.Add(GenerateTaskDomainAsync(assessment, taskItems, state, cancellationToken));

        if (habitItems.Count > 0)
            tasks.Add(GenerateHabitDomainAsync(assessment, habitItems, state, cancellationToken));

        if (experimentItems.Count > 0)
            tasks.Add(GenerateExperimentDomainAsync(assessment, experimentItems, state, state.Profile?.Preferences, state.Profile?.CurrentSeason, cancellationToken));

        if (goalMetricItems.Count > 0)
            tasks.Add(GenerateGoalMetricDomainAsync(assessment, goalMetricItems, state, state.Profile?.Values, state.Profile?.Roles, state.Profile?.CurrentSeason, cancellationToken));

        if (projectItems.Count > 0)
            tasks.Add(GenerateProjectDomainAsync(assessment, projectItems, state, cancellationToken));

        var results = await Task.WhenAll(tasks);
        foreach (var domainResults in results)
            all.AddRange(domainResults);

        return all;
    }

    private async Task<List<RecommendationCandidate>> GenerateTaskDomainAsync(
        SituationalAssessment assessment,
        List<InterventionPlanItem> interventions,
        UserStateSnapshot state,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            TaskGenerationPrompt.Model,
            TaskGenerationPrompt.BuildSystemPrompt(),
            TaskGenerationPrompt.BuildUserPrompt(
                assessment,
                interventions,
                state.Tasks,
                state.Today,
                state.Profile?.Constraints,
                state.Projects,
                state.Goals,
                state.Profile?.Roles,
                state.Profile?.Values),
            TaskGenerationPrompt.SchemaName,
            TaskGenerationPrompt.ResponseSchema,
            cancellationToken);
        if (json is null) return [];

        var candidates = _parser.ParseGenerationResponse(json, "Task");
        return RecommendationCandidateValidator.FilterInvalidEntityIds(candidates, state, _logger);
    }

    private async Task<List<RecommendationCandidate>> GenerateHabitDomainAsync(
        SituationalAssessment assessment,
        List<InterventionPlanItem> interventions,
        UserStateSnapshot state,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            HabitGenerationPrompt.Model,
            HabitGenerationPrompt.BuildSystemPrompt(),
            HabitGenerationPrompt.BuildUserPrompt(
                assessment,
                interventions,
                state.Habits,
                state.Goals,
                state.Profile?.Values,
                state.Profile?.Roles,
                state.MetricDefinitions,
                state.Today),
            HabitGenerationPrompt.SchemaName,
            HabitGenerationPrompt.ResponseSchema,
            cancellationToken);
        if (json is null) return [];

        var candidates = _parser.ParseGenerationResponse(json, "Habit");
        return RecommendationCandidateValidator.FilterInvalidEntityIds(candidates, state, _logger);
    }

    private async Task<List<RecommendationCandidate>> GenerateExperimentDomainAsync(
        SituationalAssessment assessment,
        List<InterventionPlanItem> interventions,
        UserStateSnapshot state,
        PreferencesSnapshot? preferences,
        SeasonSnapshot? season,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            ExperimentGenerationPrompt.Model,
            ExperimentGenerationPrompt.BuildSystemPrompt(),
            ExperimentGenerationPrompt.BuildUserPrompt(
                assessment,
                interventions,
                state.Experiments,
                state.MetricDefinitions,
                state.Goals,
                preferences,
                season),
            ExperimentGenerationPrompt.SchemaName,
            ExperimentGenerationPrompt.ResponseSchema,
            cancellationToken);
        if (json is null) return [];

        var candidates = _parser.ParseGenerationResponse(json, "Experiment");
        return RecommendationCandidateValidator.FilterInvalidEntityIds(candidates, state, _logger);
    }

    private async Task<List<RecommendationCandidate>> GenerateGoalMetricDomainAsync(
        SituationalAssessment assessment,
        List<InterventionPlanItem> interventions,
        UserStateSnapshot state,
        IReadOnlyList<UserValueSnapshot>? values,
        IReadOnlyList<UserRoleSnapshot>? roles,
        SeasonSnapshot? season,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            GoalMetricGenerationPrompt.Model,
            GoalMetricGenerationPrompt.BuildSystemPrompt(),
            GoalMetricGenerationPrompt.BuildUserPrompt(
                assessment,
                interventions,
                state.Goals,
                state.Projects,
                state.MetricDefinitions,
                values,
                roles,
                season),
            GoalMetricGenerationPrompt.SchemaName,
            GoalMetricGenerationPrompt.ResponseSchema,
            cancellationToken);
        if (json is null) return [];

        var candidates = _parser.ParseGenerationResponse(json, "GoalMetric");
        return RecommendationCandidateValidator.FilterInvalidEntityIds(candidates, state, _logger);
    }

    private async Task<List<RecommendationCandidate>> GenerateProjectDomainAsync(
        SituationalAssessment assessment,
        List<InterventionPlanItem> interventions,
        UserStateSnapshot state,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            ProjectGenerationPrompt.Model,
            ProjectGenerationPrompt.BuildSystemPrompt(),
            ProjectGenerationPrompt.BuildUserPrompt(
                assessment,
                interventions,
                state.Projects,
                state.Tasks,
                state.Goals,
                state.Today,
                state.Profile?.CurrentSeason,
                state.Profile?.Roles,
                state.Profile?.Values),
            ProjectGenerationPrompt.SchemaName,
            ProjectGenerationPrompt.ResponseSchema,
            cancellationToken);
        if (json is null) return [];

        var candidates = _parser.ParseGenerationResponse(json, "Project");
        return RecommendationCandidateValidator.FilterInvalidEntityIds(candidates, state, _logger);
    }

    // ─────────────────────────────────────────────────────────────────
    // OpenAI call helper
    // ─────────────────────────────────────────────────────────────────

    private async Task<string?> CallOpenAiAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        string schemaName,
        BinaryData responseSchema,
        CancellationToken cancellationToken)
    {
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

            var content = completion.Value.Content
                .Where(p => p.Kind == ChatMessageContentPartKind.Text)
                .Select(p => p.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("OpenAI returned empty content. FinishReason={FinishReason}",
                    completion.Value.FinishReason);
                return null;
            }

            _logger.LogDebug("OpenAI response ({Tokens} tokens): {Content}",
                completion.Value.Usage?.OutputTokenCount ?? 0,
                content.Length > 200 ? content[..200] + "..." : content);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI call failed");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Trace model (stored in RawResponse for explainability)
    // ─────────────────────────────────────────────────────────────────

    private sealed class PipelineTrace
    {
        [JsonPropertyName("assessment")]
        public SituationalAssessment? Assessment { get; set; }

        [JsonPropertyName("strategy")]
        public RecommendationStrategy? Strategy { get; set; }

        [JsonPropertyName("generated")]
        public List<RecommendationCandidate>? Generated { get; set; }
    }
}
