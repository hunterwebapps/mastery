namespace Mastery.Application.Features.CheckIns.Models;

/// <summary>
/// Full check-in DTO with all fields.
/// </summary>
public sealed record CheckInDto(
    Guid Id,
    string UserId,
    string CheckInDate,
    string Type,
    string Status,
    DateTime? CompletedAt,
    // Morning fields
    int? EnergyLevel,
    string? SelectedMode,
    string? Top1Type,
    Guid? Top1EntityId,
    string? Top1FreeText,
    string? Intention,
    // Evening fields
    int? EnergyLevelPm,
    int? StressLevel,
    string? Reflection,
    string? BlockerCategory,
    string? BlockerNote,
    bool? Top1Completed,
    // Audit
    DateTime CreatedAt,
    DateTime? ModifiedAt);

/// <summary>
/// Lightweight check-in for list views.
/// </summary>
public sealed record CheckInSummaryDto(
    Guid Id,
    string CheckInDate,
    string Type,
    string Status,
    int? EnergyLevel,
    int? EnergyLevelPm,
    string? SelectedMode,
    bool? Top1Completed,
    DateTime? CompletedAt);

/// <summary>
/// Today's check-in state for the daily loop view.
/// </summary>
public sealed record TodayCheckInStateDto(
    CheckInDto? MorningCheckIn,
    CheckInDto? EveningCheckIn,
    string MorningStatus,
    string EveningStatus,
    int CheckInStreakDays);
