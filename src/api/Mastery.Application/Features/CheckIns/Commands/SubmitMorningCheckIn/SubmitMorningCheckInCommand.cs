using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.CheckIns.Commands.SubmitMorningCheckIn;

/// <summary>
/// Submits a morning check-in with energy, mode, and Top 1 selection.
/// </summary>
public sealed record SubmitMorningCheckInCommand(
    int EnergyLevel,
    string SelectedMode,
    string? Top1Type = null,
    Guid? Top1EntityId = null,
    string? Top1FreeText = null,
    string? Intention = null,
    string? CheckInDate = null) : ICommand<Guid>;
