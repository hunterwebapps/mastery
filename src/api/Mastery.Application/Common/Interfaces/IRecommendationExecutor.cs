using Mastery.Domain.Entities.Recommendation;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Dispatches the appropriate domain command when a recommendation is accepted.
/// Reads ActionKind, Target.Kind, and ActionPayload to determine which MediatR command to send.
/// </summary>
public interface IRecommendationExecutor
{
    /// <summary>
    /// Executes the recommendation's action by dispatching the appropriate MediatR command.
    /// Returns the created entity ID for Create actions, or null for non-entity actions (ReflectPrompt, LearnPrompt).
    /// </summary>
    Task<Guid?> ExecuteAsync(Recommendation recommendation, CancellationToken cancellationToken = default);
}
