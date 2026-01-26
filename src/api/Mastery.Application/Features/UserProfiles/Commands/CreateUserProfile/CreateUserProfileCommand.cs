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
    ConstraintsDto? Constraints = null) : ICommand<Guid>;
