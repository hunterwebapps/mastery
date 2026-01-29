using Mastery.Api.Contracts.UserProfiles;
using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Api.Contracts.Auth;

public record RegisterWithProfileRequest(
    // Auth fields
    string Email,
    string Password,
    string? DisplayName,

    // Profile fields (from onboarding steps 1-6)
    string Timezone,
    string Locale,
    List<UserValueDto> Values,
    List<UserRoleDto> Roles,
    PreferencesDto? Preferences = null,
    ConstraintsDto? Constraints = null,
    CreateSeasonRequest? InitialSeason = null
);
