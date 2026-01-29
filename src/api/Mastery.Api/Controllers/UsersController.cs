using Mastery.Api.Contracts.Users;
using Mastery.Application.Features.Users.Commands.SetUserDisabled;
using Mastery.Application.Features.Users.Commands.UpdateUserRoles;
using Mastery.Application.Features.Users.Models;
using Mastery.Application.Features.Users.Queries.GetAllUsers;
using Mastery.Application.Features.Users.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// User management endpoints for administrators.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = "RequireAdmin")]
public class UsersController : ControllerBase
{
    private readonly ISender _mediator;

    public UsersController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all users with pagination and search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<UserListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllUsersQuery(search, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a user by ID with full details.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    /// <summary>
    /// Updates a user's roles.
    /// </summary>
    [HttpPut("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoles(
        string id,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateUserRolesCommand(id, request.Roles), cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "User not found")
            {
                return NotFound();
            }
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }

    /// <summary>
    /// Enables or disables a user.
    /// </summary>
    [HttpPut("{id}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserDisabled(
        string id,
        [FromBody] SetUserDisabledRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetUserDisabledCommand(id, request.Disabled), cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "User not found")
            {
                return NotFound();
            }
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }
}
