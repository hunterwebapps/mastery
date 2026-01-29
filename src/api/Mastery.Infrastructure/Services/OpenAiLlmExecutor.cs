using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Application.Features.Recommendations.Services;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Services.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Executes recommendation actions via OpenAI function calling.
/// Falls back to SimpleActionExecutor when OpenAI is disabled or fails.
/// </summary>
internal sealed class OpenAiLlmExecutor(
    IOptions<OpenAiOptions> options,
    IToolCallHandler toolCallHandler,
    ISimpleActionExecutor simpleExecutor,
    ILogger<OpenAiLlmExecutor> logger)
    : ILlmExecutor
{
    public async Task<ExecutionResult> ExecuteAsync(
        Recommendation recommendation,
        CancellationToken cancellationToken = default)
    {
        // Skip non-executable actions
        if (recommendation.ActionKind is RecommendationActionKind.ReflectPrompt
            or RecommendationActionKind.LearnPrompt)
        {
            return ExecutionResult.NonExecutable();
        }

        // Check if OpenAI is enabled
        if (!options.Value.Enabled || string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            logger.LogInformation("OpenAI disabled — using SimpleActionExecutor fallback");
            return await FallbackToSimpleExecutor(recommendation, cancellationToken);
        }

        try
        {
            return await ExecuteWithToolCalls(recommendation, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LLM execution failed for recommendation {Id} — trying fallback",
                recommendation.Id);
            return await FallbackToSimpleExecutor(recommendation, cancellationToken);
        }
    }

    private async Task<ExecutionResult> ExecuteWithToolCalls(
        Recommendation recommendation,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Executing recommendation {Id} via LLM tool calling: {Type} {ActionKind} {TargetKind}",
            recommendation.Id, recommendation.Type, recommendation.ActionKind, recommendation.Target.Kind);

        // Build messages
        var systemPrompt = ExecutionPrompt.BuildSystemPrompt();
        var userPrompt = ExecutionPrompt.BuildUserPrompt(recommendation);

        // Create chat client
        var client = new ChatClient(ExecutionPrompt.Model, options.Value.ApiKey);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completionOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = options.Value.MaxOutputTokens,
#pragma warning disable OPENAI001
            ReasoningEffortLevel = ChatReasoningEffortLevel.Low,
#pragma warning restore OPENAI001
        };

        // Add all tools first (must be done before setting ToolChoice)
        foreach (var tool in OpenAiLlmExecutorTools.AllTools)
        {
            completionOptions.Tools.Add(tool);
        }

        // Now set ToolChoice after tools are added
        completionOptions.ToolChoice = ChatToolChoice.CreateRequiredChoice();

        logger.LogDebug("Added {Count} tools to completion options", completionOptions.Tools.Count);

        // Set timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));

        // Call OpenAI
        var completion = await client.CompleteChatAsync(messages, completionOptions, cts.Token);

        // Check for tool calls
        var toolCalls = completion.Value.ToolCalls;
        if (toolCalls is null || toolCalls.Count == 0)
        {
            logger.LogWarning(
                "LLM returned no tool calls for recommendation {Id}. FinishReason: {FinishReason}",
                recommendation.Id, completion.Value.FinishReason);

            // Fall back to simple executor for simple actions
            return await FallbackToSimpleExecutor(recommendation, cancellationToken);
        }

        logger.LogInformation("LLM returned {Count} tool call(s) for recommendation {Id}",
            toolCalls.Count, recommendation.Id);

        // Execute tool calls
        var results = new List<ToolCallResult>();
        foreach (var toolCall in toolCalls)
        {
            var result = await toolCallHandler.ExecuteAsync(
                toolCall.FunctionName,
                toolCall.FunctionArguments.ToString(),
                cancellationToken);

            results.Add(result);

            logger.LogInformation(
                "Tool call {ToolName} completed: Success={Success}, EntityId={EntityId}, EntityKind={EntityKind}",
                toolCall.FunctionName, result.Success, result.EntityId, result.EntityKind);

            if (!result.Success)
            {
                logger.LogWarning("Tool call {ToolName} failed: {Error}",
                    toolCall.FunctionName, result.ErrorMessage);
            }
        }

        // Check if any tool call failed
        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Count > 0)
        {
            var errors = string.Join("; ", failures.Select(f => f.ErrorMessage));
            return ExecutionResult.Failed($"Tool call(s) failed: {errors}");
        }

        // Return result from the primary entity (first successful Create or the target entity)
        var primaryResult = results.FirstOrDefault(r => r.EntityId.HasValue)
            ?? results.FirstOrDefault();

        if (primaryResult is null)
        {
            return ExecutionResult.Failed("No tool calls were executed");
        }

        return ExecutionResult.ForServerExecuted(
            primaryResult.EntityId ?? recommendation.Target.EntityId ?? Guid.Empty,
            primaryResult.EntityKind ?? recommendation.Target.Kind.ToString());
    }

    private async Task<ExecutionResult> FallbackToSimpleExecutor(
        Recommendation recommendation,
        CancellationToken cancellationToken)
    {
        // Try simple executor for basic actions
        if (simpleExecutor.CanExecute(recommendation.ActionKind))
        {
            logger.LogInformation("Using SimpleActionExecutor for {ActionKind}", recommendation.ActionKind);
            return await simpleExecutor.ExecuteAsync(recommendation, cancellationToken);
        }

        // For actions that require LLM but LLM is unavailable, return failure
        logger.LogWarning(
            "Cannot execute recommendation {Id} ({ActionKind} {TargetKind}): LLM unavailable and SimpleActionExecutor doesn't support this action",
            recommendation.Id, recommendation.ActionKind, recommendation.Target.Kind);

        return ExecutionResult.Failed(
            $"Cannot execute {recommendation.ActionKind} action without LLM executor. " +
            "Please ensure OpenAI is configured or accept this action manually.");
    }
}
