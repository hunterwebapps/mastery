using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Application.Features.UserProfiles.Queries.GetCurrentUserProfile;

/// <summary>
/// Gets the current authenticated user's profile.
/// </summary>
public sealed record GetCurrentUserProfileQuery : IQuery<UserProfileDto?>;
