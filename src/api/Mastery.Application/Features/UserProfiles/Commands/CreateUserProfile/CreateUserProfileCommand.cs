using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Application.Features.UserProfiles.Commands.CreateUserProfile;

/// <summary>
/// Creates a new user profile during onboarding.
/// </summary>
public sealed record CreateUserProfileCommand(
    string Timezone,
    string Locale,
    List<UserValueDto> Values,
    List<UserRoleDto> Roles,
    PreferencesDto? Preferences = null,
    ConstraintsDto? Constraints = null,
    InitialSeasonDto? InitialSeason = null) : ICommand<Guid>;

/// <summary>
/// Initial season data for onboarding.
/// </summary>
public sealed record InitialSeasonDto(
    string Label,
    string Type,
    DateOnly StartDate,
    DateOnly? ExpectedEndDate = null,
    List<Guid>? FocusRoleIds = null,
    List<Guid>? FocusGoalIds = null,
    string? SuccessStatement = null,
    List<string>? NonNegotiables = null,
    int Intensity = 3);
