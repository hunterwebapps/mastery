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
    IOptions<RagOptions> _ragOptions,
    IRagContextRetriever _ragRetriever,
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

        // ── RAG for Stage 1 ────────────────────────────────────────────
        _logger.LogDebug("Retrieving RAG context for assessment stage");
        var assessmentRag = await _ragRetriever.RetrieveForAssessmentAsync(state, context, cancellationToken);
        trace.AssessmentRag = assessmentRag;

        // ── Stage 1: Situational Assessment ────────────────────────────
        _logger.LogInformation("Stage 1: Running situational assessment for context {Context}", context);

        var assessment = await RunStage1AssessmentAsync(state, context, assessmentRag, cancellationToken);
        if (assessment is null)
        {
            _logger.LogWarning("Stage 1 failed — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Stage1-Failed");
        }
        trace.Assessment = assessment;

        // ── RAG for Stage 2 ────────────────────────────────────────────
        _logger.LogDebug("Retrieving RAG context for strategy stage");
        var strategyRag = await _ragRetriever.RetrieveForStrategyAsync(assessment, context, state.UserId, cancellationToken);
        trace.StrategyRag = strategyRag;

        // ── Stage 2: Recommendation Strategy ───────────────────────────
        _logger.LogInformation("Stage 2: Building recommendation strategy");

        var stage2Result = await RunStage2StrategyAsync(
            assessment, context, state.Profile, strategyRag, state.Today, state.UserId, cancellationToken);
        trace.AgenticSearchMetrics = stage2Result.Metrics;

        if (stage2Result.Strategy is null)
        {
            _logger.LogWarning("Stage 2 failed — returning empty results");
            return new RecommendationOrchestrationResult(
                SelectedCandidates: [],
                SelectionMethod: "OpenAI-Stage2-Failed");
        }
        var strategy = stage2Result.Strategy;
        trace.Strategy = strategy;

        // ── Stage 3: Parallel Domain Generation ────────────────────────
        _logger.LogInformation("Stage 3: Generating recommendations across domains (parallel)");

        var generated = await RunStage3GenerationAsync(
            assessment, strategy, state, trace, cancellationToken);

        // Remove conflicting/overlapping recommendations
        generated = RemoveConflictingRecommendations(generated);
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
        RagContext? ragContext,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            AssessmentPrompt.Model,
            AssessmentPrompt.BuildSystemPrompt(context),
            AssessmentPrompt.BuildUserPrompt(state, context, ragContext),
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

    private record Stage2Result(
        RecommendationStrategy? Strategy,
        AgenticSearchMetrics Metrics);

    private async Task<Stage2Result> RunStage2StrategyAsync(
        SituationalAssessment assessment,
        RecommendationContext context,
        UserProfileSnapshot? profile,
        RagContext? ragContext,
        DateOnly today,
        string userId,
        CancellationToken cancellationToken)
    {
        // Check if agentic search is enabled
        var ragOptions = _ragOptions.Value;
        if (!ragOptions.EnableAgenticSearch)
        {
            // Standard path: no tool calling
            var strategy = await RunStage2StrategyStandardAsync(
                assessment, context, profile, ragContext, today, cancellationToken);
            return new Stage2Result(strategy, new AgenticSearchMetrics { Enabled = false });
        }

        // Agentic path: with search_history tool
        return await RunStage2StrategyWithToolsAsync(
            assessment, context, profile, ragContext, today, userId, cancellationToken);
    }

    private async Task<RecommendationStrategy?> RunStage2StrategyStandardAsync(
        SituationalAssessment assessment,
        RecommendationContext context,
        UserProfileSnapshot? profile,
        RagContext? ragContext,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var json = await CallOpenAiAsync(
            StrategyPrompt.Model,
            StrategyPrompt.BuildSystemPrompt(context),
            StrategyPrompt.BuildUserPrompt(assessment, context, profile, ragContext, today),
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

    private async Task<Stage2Result> RunStage2StrategyWithToolsAsync(
        SituationalAssessment assessment,
        RecommendationContext context,
        UserProfileSnapshot? profile,
        RagContext? ragContext,
        DateOnly today,
        string userId,
        CancellationToken cancellationToken)
    {
        var metrics = new AgenticSearchMetrics { Enabled = true };

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(StrategyPrompt.BuildSystemPrompt(context)),
            new UserChatMessage(StrategyPrompt.BuildUserPrompt(assessment, context, profile, ragContext, today))
        };

        var completionOptions = new ChatCompletionOptions
        {
#pragma warning disable OPENAI001
            ReasoningEffortLevel = ChatReasoningEffortLevel.Medium,
#pragma warning restore OPENAI001
            MaxOutputTokenCount = _options.Value.MaxOutputTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: StrategyPrompt.SchemaName,
                jsonSchema: StrategyPrompt.ResponseSchema,
                jsonSchemaIsStrict: true)
        };

        // Add search_history tool
        foreach (var tool in OpenAiLlmOrchestratorTools.StrategyTools)
            completionOptions.Tools.Add(tool);
        completionOptions.ToolChoice = ChatToolChoice.CreateAutoChoice();

        var client = new ChatClient(StrategyPrompt.Model, _options.Value.ApiKey);
        var searchCallCount = 0;
        var maxSearchCalls = _ragOptions.Value.MaxAgenticSearchCalls;
        var turnCount = 0;
        const int maxTurns = 5; // Safety limit to prevent infinite loops

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.Value.TimeoutSeconds * 2)); // Extended timeout for tool calls

        try
        {
            while (turnCount < maxTurns)
            {
                turnCount++;
                var completion = await client.CompleteChatAsync(messages, completionOptions, cts.Token);
                var response = completion.Value;

                // If finished with no tool calls, parse and return
                if (response.FinishReason == ChatFinishReason.Stop)
                {
                    var json = response.Content
                        .Where(p => p.Kind == ChatMessageContentPartKind.Text)
                        .Select(p => p.Text)
                        .FirstOrDefault();

                    _logger.LogInformation(
                        "Stage 2 completed with tools: SearchCalls={SearchCalls}, Turns={Turns}",
                        searchCallCount, turnCount);

                    metrics.SearchCallCount = searchCallCount;

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _logger.LogWarning("Stage 2 returned empty content after tool calls");
                        return new Stage2Result(null, metrics);
                    }

                    try
                    {
                        var strategy = JsonSerializer.Deserialize<RecommendationStrategy>(json, JsonOptions);
                        return new Stage2Result(strategy, metrics);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize Stage 2 strategy response after tool calls");
                        return new Stage2Result(null, metrics);
                    }
                }

                // Handle tool calls
                if (response.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Add assistant message with tool calls to conversation
                    messages.Add(new AssistantChatMessage(response));

                    foreach (var toolCall in response.ToolCalls)
                    {
                        if (toolCall.FunctionName == "search_history")
                        {
                            var toolResult = await HandleSearchHistoryToolAsync(
                                toolCall, userId, today, searchCallCount, maxSearchCalls, cts.Token);
                            messages.Add(new ToolChatMessage(toolCall.Id, toolResult.Content));

                            if (toolResult.WasExecuted)
                            {
                                searchCallCount++;
                                metrics.SearchQueries.Add(toolResult.Query ?? "");
                            }
                        }
                        else
                        {
                            // Unknown tool
                            _logger.LogWarning("Unknown tool call: {ToolName}", toolCall.FunctionName);
                            messages.Add(new ToolChatMessage(toolCall.Id, "Unknown tool. Please proceed with available context."));
                        }
                    }
                }
                else
                {
                    // Unexpected finish reason
                    _logger.LogWarning("Stage 2 unexpected finish reason: {Reason}", response.FinishReason);
                    metrics.SearchCallCount = searchCallCount;
                    return new Stage2Result(null, metrics);
                }
            }

            _logger.LogWarning("Stage 2 exceeded max turns ({MaxTurns})", maxTurns);
            metrics.SearchCallCount = searchCallCount;
            return new Stage2Result(null, metrics);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Stage 2 with tools timed out after {Turns} turns", turnCount);
            metrics.SearchCallCount = searchCallCount;
            return new Stage2Result(null, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stage 2 with tools failed after {Turns} turns", turnCount);
            metrics.SearchCallCount = searchCallCount;
            return new Stage2Result(null, metrics);
        }
    }

    private async Task<(string Content, bool WasExecuted, string? Query)> HandleSearchHistoryToolAsync(
        ChatToolCall toolCall,
        string userId,
        DateOnly today,
        int currentSearchCount,
        int maxSearchCalls,
        CancellationToken cancellationToken)
    {
        // Check if we've exceeded the search limit
        if (currentSearchCount >= maxSearchCalls)
        {
            _logger.LogDebug("search_history call rejected: limit reached ({Current}/{Max})",
                currentSearchCount, maxSearchCalls);
            return ("Search limit reached. Please proceed with available context.", false, null);
        }

        // Parse arguments
        SearchHistoryArgs args;
        try
        {
            args = JsonSerializer.Deserialize<SearchHistoryArgs>(
                toolCall.FunctionArguments.ToString(),
                JsonOptions) ?? new SearchHistoryArgs("");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse search_history arguments");
            return ("Invalid arguments. Please provide a valid query string.", false, null);
        }

        if (string.IsNullOrWhiteSpace(args.Query))
        {
            return ("Empty query. Please provide a search query.", false, null);
        }

        _logger.LogInformation(
            "search_history tool called: query='{Query}', entityTypes={EntityTypes}, maxResults={MaxResults}",
            args.Query,
            args.EntityTypes is not null ? string.Join(",", args.EntityTypes) : "all",
            args.MaxResults ?? 5);

        // Execute the search
        var additionalContext = await _ragRetriever.SearchAsync(
            userId,
            args.Query,
            args.EntityTypes,
            args.MaxResults ?? 5,
            cancellationToken);

        if (additionalContext is null || additionalContext.Items.Count == 0)
        {
            _logger.LogDebug("search_history returned no results for query: {Query}", args.Query);
            return ("No additional results found matching your query.", true, args.Query);
        }

        // Format the results for the LLM
        var resultText = RagContextFormatter.FormatForStrategy(additionalContext, today)
            ?? "No relevant context found.";

        _logger.LogInformation(
            "search_history returned {Count} results for query: {Query}",
            additionalContext.Items.Count, args.Query);

        return (resultText, true, args.Query);
    }

    // ─────────────────────────────────────────────────────────────────
    // Stage 3 — parallel domain generation
    // ─────────────────────────────────────────────────────────────────

    private async Task<List<RecommendationCandidate>> RunStage3GenerationAsync(
        SituationalAssessment assessment,
        RecommendationStrategy strategy,
        UserStateSnapshot state,
        PipelineTrace trace,
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
            { "ProjectStuckFix", "ProjectSuggestion", "ProjectEditSuggestion", "ProjectArchiveSuggestion", "ProjectGoalLinkSuggestion" };

        var taskItems = plan.Where(i => taskTypes.Contains(i.TargetType)).ToList();
        var habitItems = plan.Where(i => habitTypes.Contains(i.TargetType)).ToList();
        var experimentItems = plan.Where(i => experimentTypes.Contains(i.TargetType)).ToList();
        var goalMetricItems = plan.Where(i => goalMetricTypes.Contains(i.TargetType)).ToList();
        var projectItems = plan.Where(i => projectTypes.Contains(i.TargetType)).ToList();

        // Retrieve RAG context for each domain in parallel (if enabled)
        var ragTasks = new Dictionary<string, Task<RagContext?>>();

        if (taskItems.Count > 0)
            ragTasks["Task"] = _ragRetriever.RetrieveForGenerationAsync("Task", assessment, taskItems, state, cancellationToken);
        if (habitItems.Count > 0)
            ragTasks["Habit"] = _ragRetriever.RetrieveForGenerationAsync("Habit", assessment, habitItems, state, cancellationToken);
        if (experimentItems.Count > 0)
            ragTasks["Experiment"] = _ragRetriever.RetrieveForGenerationAsync("Experiment", assessment, experimentItems, state, cancellationToken);
        if (goalMetricItems.Count > 0)
            ragTasks["GoalMetric"] = _ragRetriever.RetrieveForGenerationAsync("GoalMetric", assessment, goalMetricItems, state, cancellationToken);
        if (projectItems.Count > 0)
            ragTasks["Project"] = _ragRetriever.RetrieveForGenerationAsync("Project", assessment, projectItems, state, cancellationToken);

        // Wait for all RAG retrievals
        await Task.WhenAll(ragTasks.Values);

        // Get RAG context for each domain
        var ragContexts = new Dictionary<string, RagContext?>();
        foreach (var kvp in ragTasks)
            ragContexts[kvp.Key] = await kvp.Value;

        // Store RAG contexts in trace
        trace.GenerationRag = ragContexts.Values.Where(r => r is not null).ToList()!;

        // Launch domain prompts in parallel
        var generationTasks = new List<Task<List<RecommendationCandidate>>>();

        if (taskItems.Count > 0)
            generationTasks.Add(GenerateTaskDomainAsync(assessment, taskItems, state, ragContexts.GetValueOrDefault("Task"), cancellationToken));

        if (habitItems.Count > 0)
            generationTasks.Add(GenerateHabitDomainAsync(assessment, habitItems, state, ragContexts.GetValueOrDefault("Habit"), cancellationToken));

        if (experimentItems.Count > 0)
            generationTasks.Add(GenerateExperimentDomainAsync(assessment, experimentItems, state, state.Profile?.Preferences, state.Profile?.CurrentSeason, ragContexts.GetValueOrDefault("Experiment"), cancellationToken));

        if (goalMetricItems.Count > 0)
            generationTasks.Add(GenerateGoalMetricDomainAsync(assessment, goalMetricItems, state, state.Profile?.Values, state.Profile?.Roles, state.Profile?.CurrentSeason, ragContexts.GetValueOrDefault("GoalMetric"), cancellationToken));

        if (projectItems.Count > 0)
            generationTasks.Add(GenerateProjectDomainAsync(assessment, projectItems, state, ragContexts.GetValueOrDefault("Project"), cancellationToken));

        var results = await Task.WhenAll(generationTasks);
        foreach (var domainResults in results)
            all.AddRange(domainResults);

        return all;
    }

    private async Task<List<RecommendationCandidate>> GenerateTaskDomainAsync(
        SituationalAssessment assessment,
        List<InterventionPlanItem> interventions,
        UserStateSnapshot state,
        RagContext? ragContext,
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
                state.Profile?.Values,
                ragContext),
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
        RagContext? ragContext,
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
                state.Today,
                ragContext),
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
        RagContext? ragContext,
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
                season,
                ragContext),
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
        RagContext? ragContext,
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
                season,
                ragContext),
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
        RagContext? ragContext,
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
                state.Profile?.Values,
                ragContext),
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
    // Conflict detection and deduplication
    // ─────────────────────────────────────────────────────────────────

    private List<RecommendationCandidate> RemoveConflictingRecommendations(List<RecommendationCandidate> candidates)
    {
        if (candidates.Count <= 1)
            return candidates;

        var result = new List<RecommendationCandidate>();
        var seenTargets = new HashSet<(RecommendationType Type, Guid? EntityId)>();
        var hasCheckInNudge = false;
        var hasCheckInExperiment = false;
        var habitBehaviors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var experimentBehaviors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Sort by score descending so higher-scored recommendations are kept
        var sorted = candidates.OrderByDescending(c => c.Score).ToList();

        foreach (var candidate in sorted)
        {
            // Rule 1: No duplicate (Type, EntityId) combinations
            var key = (candidate.Type, candidate.Target.EntityId);
            if (seenTargets.Contains(key))
            {
                _logger.LogDebug(
                    "Removed duplicate recommendation: {Type} for entity {EntityId}",
                    candidate.Type, candidate.Target.EntityId);
                continue;
            }

            // Rule 2: No CheckInConsistencyNudge if there's already an experiment about check-ins
            if (candidate.Type == Domain.Enums.RecommendationType.CheckInConsistencyNudge)
            {
                if (hasCheckInExperiment)
                {
                    _logger.LogDebug("Removed CheckInConsistencyNudge due to existing check-in experiment");
                    continue;
                }
                hasCheckInNudge = true;
            }

            // Rule 3: No experiment about check-ins if there's already a CheckInConsistencyNudge
            if (candidate.Type == Domain.Enums.RecommendationType.ExperimentRecommendation)
            {
                var titleLower = candidate.Title.ToLowerInvariant();
                if (titleLower.Contains("check-in") || titleLower.Contains("checkin"))
                {
                    if (hasCheckInNudge)
                    {
                        _logger.LogDebug("Removed check-in experiment due to existing CheckInConsistencyNudge");
                        continue;
                    }
                    hasCheckInExperiment = true;
                }
            }

            // Rule 4: No HabitFromLeadMetricSuggestion + ExperimentRecommendation for same behavior
            if (candidate.Type == Domain.Enums.RecommendationType.HabitFromLeadMetricSuggestion)
            {
                var behavior = ExtractBehaviorKeyword(candidate.Title);
                if (experimentBehaviors.Contains(behavior))
                {
                    _logger.LogDebug("Removed HabitFromLeadMetricSuggestion due to existing experiment for '{Behavior}'", behavior);
                    continue;
                }
                habitBehaviors.Add(behavior);
            }

            if (candidate.Type == Domain.Enums.RecommendationType.ExperimentRecommendation)
            {
                var behavior = ExtractBehaviorKeyword(candidate.Title);
                if (habitBehaviors.Contains(behavior))
                {
                    _logger.LogDebug("Removed ExperimentRecommendation due to existing habit suggestion for '{Behavior}'", behavior);
                    continue;
                }
                experimentBehaviors.Add(behavior);
            }

            seenTargets.Add(key);
            result.Add(candidate);
        }

        if (result.Count < candidates.Count)
        {
            _logger.LogInformation(
                "Removed {Count} conflicting recommendations ({Before} -> {After})",
                candidates.Count - result.Count, candidates.Count, result.Count);
        }

        return result;
    }

    private static string ExtractBehaviorKeyword(string title)
    {
        // Extract a normalized behavior keyword from the title for conflict matching
        // e.g., "Create daily outreach habit" -> "outreach"
        // e.g., "Experiment: Daily outreach tracking" -> "outreach"
        var normalized = title.ToLowerInvariant()
            .Replace("experiment:", "")
            .Replace("create", "")
            .Replace("daily", "")
            .Replace("habit", "")
            .Replace("suggestion", "")
            .Trim();

        // Return first significant word (>3 chars)
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.FirstOrDefault(w => w.Length > 3) ?? normalized;
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

        [JsonPropertyName("assessmentRag")]
        public RagContext? AssessmentRag { get; set; }

        [JsonPropertyName("strategyRag")]
        public RagContext? StrategyRag { get; set; }

        [JsonPropertyName("generationRag")]
        public List<RagContext>? GenerationRag { get; set; }

        [JsonPropertyName("agenticSearchMetrics")]
        public AgenticSearchMetrics? AgenticSearchMetrics { get; set; }
    }

    private sealed class AgenticSearchMetrics
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("searchCallCount")]
        public int SearchCallCount { get; set; }

        [JsonPropertyName("searchQueries")]
        public List<string> SearchQueries { get; set; } = [];
    }
}
