using Mastery.Api.Contracts.UserProfiles;
using Mastery.Application.Features.Seasons.Commands.CreateSeason;
using Mastery.Application.Features.Seasons.Queries.GetUserSeasons;
using Mastery.Application.Features.UserProfiles.Commands.CreateUserProfile;
using Mastery.Application.Features.UserProfiles.Models;
using Mastery.Application.Features.UserProfiles.Queries.GetCurrentUserProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages user profiles and seasons.
/// </summary>
[ApiController]
[Route("api/user-profile")]
public class UserProfilesController : ControllerBase
{
    private readonly ISender _mediator;

    public UserProfilesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentProfile(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserProfileQuery(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new user profile (onboarding).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProfile(
        [FromBody] CreateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserProfileCommand(
            request.Timezone,
            request.Locale,
            request.Values,
            request.Roles,
            request.Preferences,
            request.Constraints);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCurrentProfile), new { id }, id);
    }

    /// <summary>
    /// Updates user preferences.
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] PreferencesDto preferences,
        CancellationToken cancellationToken)
    {
        // TODO: Implement UpdatePreferencesCommand
        await Task.CompletedTask;
        return NoContent();
    }

    /// <summary>
    /// Updates user constraints.
    /// </summary>
    [HttpPut("constraints")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConstraints(
        [FromBody] ConstraintsDto constraints,
        CancellationToken cancellationToken)
    {
        // TODO: Implement UpdateConstraintsCommand
        await Task.CompletedTask;
        return NoContent();
    }

    /// <summary>
    /// Updates user values.
    /// </summary>
    [HttpPut("values")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateValues(
        [FromBody] UpdateValuesRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: Implement UpdateValuesCommand
        await Task.CompletedTask;
        return NoContent();
    }

    /// <summary>
    /// Updates user roles.
    /// </summary>
    [HttpPut("roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoles(
        [FromBody] UpdateRolesRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: Implement UpdateRolesCommand
        await Task.CompletedTask;
        return NoContent();
    }

    /// <summary>
    /// Gets all seasons for the current user.
    /// </summary>
    [HttpGet("seasons")]
    [ProducesResponseType(typeof(IReadOnlyList<SeasonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeasons(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserSeasonsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new season (becomes the current season).
    /// </summary>
    [HttpPost("seasons")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSeason(
        [FromBody] CreateSeasonRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSeasonCommand(
            request.Label,
            request.Type,
            request.StartDate,
            request.ExpectedEndDate,
            request.FocusRoleIds,
            request.FocusGoalIds,
            request.SuccessStatement,
            request.NonNegotiables,
            request.Intensity);

        var id = await _mediator.Send(command, cancellationToken);
        return Created($"/api/user-profile/seasons/{id}", id);
    }

    /// <summary>
    /// Ends a season with an optional outcome.
    /// </summary>
    [HttpPut("seasons/{id:guid}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndSeason(
        Guid id,
        [FromBody] EndSeasonRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: Implement EndSeasonCommand
        await Task.CompletedTask;
        return NoContent();
    }
}
