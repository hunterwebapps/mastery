namespace Mastery.Api.Contracts.CheckIns;

/// <summary>
/// Request to submit a morning check-in.
/// </summary>
public sealed record SubmitMorningCheckInRequest(
    int EnergyLevel,
    string SelectedMode,
    string? Top1Type = null,
    Guid? Top1EntityId = null,
    string? Top1FreeText = null,
    string? Intention = null,
    string? CheckInDate = null);

/// <summary>
/// Request to submit an evening check-in.
/// </summary>
public sealed record SubmitEveningCheckInRequest(
    bool? Top1Completed = null,
    int? EnergyLevelPm = null,
    int? StressLevel = null,
    string? Reflection = null,
    string? BlockerCategory = null,
    string? BlockerNote = null,
    string? CheckInDate = null);

/// <summary>
/// Request to update an existing check-in.
/// </summary>
public sealed record UpdateCheckInRequest(
    // Morning fields
    int? EnergyLevel = null,
    string? SelectedMode = null,
    string? Top1Type = null,
    Guid? Top1EntityId = null,
    string? Top1FreeText = null,
    string? Intention = null,
    // Evening fields
    bool? Top1Completed = null,
    int? EnergyLevelPm = null,
    int? StressLevel = null,
    string? Reflection = null,
    string? BlockerCategory = null,
    string? BlockerNote = null);

/// <summary>
/// Request to skip a check-in.
/// </summary>
public sealed record SkipCheckInRequest(
    string Type,
    string? CheckInDate = null);
