using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Seasons.Commands.EndSeason;

/// <summary>
/// Ends a season with an optional outcome reflection.
/// </summary>
public sealed record EndSeasonCommand(
    Guid SeasonId,
    string? Outcome = null) : ICommand;
