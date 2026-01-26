using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Application.Features.Seasons.Queries.GetUserSeasons;

/// <summary>
/// Gets all seasons for the current user, ordered by start date (most recent first).
/// </summary>
public sealed record GetUserSeasonsQuery : IQuery<IReadOnlyList<SeasonDto>>;
