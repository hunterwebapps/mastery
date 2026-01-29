namespace Mastery.Application.Features.Recommendations.Models;

/// <summary>
/// Result of executing or accepting a recommendation.
/// For server-side executed actions (ExecuteToday, Defer), contains the entity ID.
/// For client-side form actions (Create, Update, Remove), contains payload for form pre-population.
/// </summary>
public sealed record ExecutionResult
{
    public bool Success { get; init; }
    public Guid? EntityId { get; init; }
    public string? EntityKind { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// JSON payload for client-side form pre-population.
    /// Contains the actionPayload from the recommendation.
    /// </summary>
    public string? ActionPayload { get; init; }

    /// <summary>
    /// The action kind (Create, Update, Remove, ExecuteToday, Defer, etc.)
    /// </summary>
    public string? ActionKind { get; init; }

    /// <summary>
    /// The target entity kind (Task, Habit, Goal, etc.)
    /// </summary>
    public string? TargetKind { get; init; }

    /// <summary>
    /// The target entity ID for Update/Remove actions.
    /// </summary>
    public Guid? TargetEntityId { get; init; }

    /// <summary>
    /// True for Create/Update/Remove actions that require client-side form navigation.
    /// False for server-side executed actions (ExecuteToday, Defer) and non-executable actions.
    /// </summary>
    public bool RequiresClientAction { get; init; }

    /// <summary>
    /// Creates a result for server-side executed actions.
    /// </summary>
    public static ExecutionResult ForServerExecuted(Guid id, string kind) =>
        new()
        {
            Success = true,
            EntityId = id,
            EntityKind = kind,
            RequiresClientAction = false
        };

    /// <summary>
    /// Creates a result for client-side form pre-population.
    /// </summary>
    public static ExecutionResult ForClientAction(
        string actionPayload,
        string actionKind,
        string targetKind,
        Guid? targetEntityId = null) =>
        new()
        {
            Success = true,
            RequiresClientAction = true,
            ActionPayload = actionPayload,
            ActionKind = actionKind,
            TargetKind = targetKind,
            TargetEntityId = targetEntityId
        };

    /// <summary>
    /// Creates a result for non-executable actions (ReflectPrompt, LearnPrompt).
    /// </summary>
    public static ExecutionResult NonExecutable() =>
        new()
        {
            Success = true,
            RequiresClientAction = false
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ExecutionResult Failed(string error) =>
        new()
        {
            Success = false,
            ErrorMessage = error,
            RequiresClientAction = false
        };
}
