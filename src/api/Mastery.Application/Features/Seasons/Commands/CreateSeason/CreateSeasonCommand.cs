using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Seasons.Commands.CreateSeason;

/// <summary>
/// Creates a new season and sets it as the current season for the user.
/// </summary>
public sealed record CreateSeasonCommand(
    string Label,
    string Type,
    DateOnly StartDate,
    DateOnly? ExpectedEndDate = null,
    List<Guid>? FocusRoleIds = null,
    List<Guid>? FocusGoalIds = null,
    string? SuccessStatement = null,
    List<string>? NonNegotiables = null,
    int Intensity = 3) : ICommand<Guid>;
