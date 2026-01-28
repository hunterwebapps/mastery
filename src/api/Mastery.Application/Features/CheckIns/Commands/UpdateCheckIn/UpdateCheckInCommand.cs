using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.CheckIns.Commands.UpdateCheckIn;

/// <summary>
/// Updates an existing check-in.
/// </summary>
public sealed record UpdateCheckInCommand(
    Guid Id,
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
    string? BlockerNote = null) : ICommand;
