using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Entities.Recommendation;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Executes recommendation actions via LLM tool calling.
/// Uses OpenAI function calling to parse ActionPayload and dispatch appropriate MediatR commands.
/// </summary>
public interface ILlmExecutor
{
    /// <summary>
    /// Executes the recommendation's action using LLM tool calling.
    /// The LLM parses the ActionPayload and returns tool calls that are executed via MediatR.
    /// </summary>
    /// <param name="recommendation">The recommendation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result with entity ID(s) and success status.</returns>
    Task<ExecutionResult> ExecuteAsync(Recommendation recommendation, CancellationToken cancellationToken = default);
}
