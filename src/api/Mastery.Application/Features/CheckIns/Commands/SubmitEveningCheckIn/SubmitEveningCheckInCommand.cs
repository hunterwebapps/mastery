using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.CheckIns.Commands.SubmitEveningCheckIn;

/// <summary>
/// Submits an evening check-in with completion review, blocker, and reflection.
/// </summary>
public sealed record SubmitEveningCheckInCommand(
    bool? Top1Completed = null,
    int? EnergyLevelPm = null,
    int? StressLevel = null,
    string? Reflection = null,
    string? BlockerCategory = null,
    string? BlockerNote = null,
    string? CheckInDate = null) : ICommand<Guid>;
