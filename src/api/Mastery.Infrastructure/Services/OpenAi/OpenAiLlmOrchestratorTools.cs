using OpenAI.Chat;

namespace Mastery.Infrastructure.Services.OpenAi;

/// <summary>
/// Tool definitions for the LLM orchestrator pipeline.
/// These tools allow the LLM to request additional context when needed.
/// </summary>
internal static class OpenAiLlmOrchestratorTools
{
    /// <summary>
    /// Allows the LLM to search user's historical context for specific patterns,
    /// past interventions, or experiments when pre-fetched RAG context is insufficient.
    /// </summary>
    public static ChatTool SearchHistory { get; } = ChatTool.CreateFunctionTool(
        functionName: "search_history",
        functionDescription: "Search user's historical context for specific patterns, past interventions, or experiments. Use when pre-fetched context is insufficient for your strategy decision.",
        functionParameters: BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                query = new
                {
                    type = "string",
                    description = "Natural language semantic search query (e.g., 'times when user successfully recovered from habit slipping', 'experiments that improved morning routine', 'dismissed recommendations about exercise')"
                },
                entityTypes = new
                {
                    type = "array",
                    items = new
                    {
                        type = "string",
                        @enum = new[] { "Recommendation", "Experiment", "CheckIn", "Goal", "Habit", "Task", "Project" }
                    },
                    description = "Optional filter to specific entity types. If not provided, searches all types."
                },
                maxResults = new
                {
                    type = "integer",
                    minimum = 1,
                    maximum = 10,
                    @default = 5,
                    description = "Maximum number of results to return (1-10). Default is 5."
                }
            },
            required = new[] { "query" },
            additionalProperties = false
        }));

    /// <summary>
    /// All tools available for the Strategy stage.
    /// </summary>
    public static IReadOnlyList<ChatTool> StrategyTools { get; } = [SearchHistory];
}

/// <summary>
/// Arguments for the search_history tool.
/// </summary>
internal sealed record SearchHistoryArgs(
    string Query,
    string[]? EntityTypes = null,
    int? MaxResults = null);
