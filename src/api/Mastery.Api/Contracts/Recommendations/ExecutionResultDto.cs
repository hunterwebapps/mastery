namespace Mastery.Api.Contracts.Recommendations;

/// <summary>
/// Result of accepting a recommendation, returned to the frontend for handling.
/// For server-side executed actions: contains EntityId.
/// For client-side form actions: contains ActionPayload for form pre-population.
/// </summary>
public sealed record ExecutionResultDto(
    string? EntityId,
    string? EntityKind,
    bool Success,
    string? ErrorMessage,
    string? ActionPayload,
    string? ActionKind,
    string? TargetKind,
    string? TargetEntityId,
    bool RequiresClientAction);
