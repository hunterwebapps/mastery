using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Api.Contracts.UserProfiles;

/// <summary>
/// Request to create a new user profile (onboarding).
/// </summary>
public sealed record CreateUserProfileRequest(
    string Timezone,
    string Locale,
    List<UserValueDto> Values,
    List<UserRoleDto> Roles,
    PreferencesDto? Preferences = null,
    ConstraintsDto? Constraints = null);

/// <summary>
/// Request to update user values.
/// </summary>
public sealed record UpdateValuesRequest(List<UserValueDto> Values);

/// <summary>
/// Request to update user roles.
/// </summary>
public sealed record UpdateRolesRequest(List<UserRoleDto> Roles);

/// <summary>
/// Request to create a new season.
/// </summary>
public sealed record CreateSeasonRequest(
    string Label,
    string Type,
    DateOnly StartDate,
    DateOnly? ExpectedEndDate = null,
    List<Guid>? FocusRoleIds = null,
    List<Guid>? FocusGoalIds = null,
    string? SuccessStatement = null,
    List<string>? NonNegotiables = null,
    int Intensity = 3);

/// <summary>
/// Request to end a season.
/// </summary>
public sealed record EndSeasonRequest(string? Outcome = null);
