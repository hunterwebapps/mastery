using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.CheckIns.Commands.SkipCheckIn;

/// <summary>
/// Skips a check-in for a given date and type.
/// </summary>
public sealed record SkipCheckInCommand(
    string Type,
    string? CheckInDate = null) : ICommand<Guid>;
