using Mastery.Api.Contracts.CheckIns;
using Mastery.Application.Features.CheckIns.Commands.SkipCheckIn;
using Mastery.Application.Features.CheckIns.Commands.SubmitEveningCheckIn;
using Mastery.Application.Features.CheckIns.Commands.SubmitMorningCheckIn;
using Mastery.Application.Features.CheckIns.Commands.UpdateCheckIn;
using Mastery.Application.Features.CheckIns.Models;
using Mastery.Application.Features.CheckIns.Queries.GetCheckInById;
using Mastery.Application.Features.CheckIns.Queries.GetCheckIns;
using Mastery.Application.Features.CheckIns.Queries.GetTodayCheckInState;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages daily check-ins (morning and evening) for the control loop.
/// </summary>
[ApiController]
[Route("api/check-ins")]
public class CheckInsController : ControllerBase
{
    private readonly ISender _mediator;

    public CheckInsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Submits a morning check-in with energy, mode, and Top 1 selection.
    /// </summary>
    [HttpPost("morning")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitMorning(
        [FromBody] SubmitMorningCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitMorningCheckInCommand(
            request.EnergyLevel,
            request.SelectedMode,
            request.Top1Type,
            request.Top1EntityId,
            request.Top1FreeText,
            request.Intention,
            request.CheckInDate);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Submits an evening check-in with completion review, blocker, and reflection.
    /// </summary>
    [HttpPost("evening")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitEvening(
        [FromBody] SubmitEveningCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitEveningCheckInCommand(
            request.Top1Completed,
            request.EnergyLevelPm,
            request.StressLevel,
            request.Reflection,
            request.BlockerCategory,
            request.BlockerNote,
            request.CheckInDate);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Gets today's check-in state for the daily loop view.
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(TodayCheckInStateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodayState(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTodayCheckInStateQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets check-in history, optionally filtered by date range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CheckInSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCheckIns(
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCheckInsQuery(fromDate, toDate), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single check-in by ID with full details.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CheckInDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCheckInByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing check-in.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCheckInCommand(
            id,
            request.EnergyLevel,
            request.SelectedMode,
            request.Top1Type,
            request.Top1EntityId,
            request.Top1FreeText,
            request.Intention,
            request.Top1Completed,
            request.EnergyLevelPm,
            request.StressLevel,
            request.Reflection,
            request.BlockerCategory,
            request.BlockerNote);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Skips a check-in for a given date and type.
    /// </summary>
    [HttpPost("skip")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Skip(
        [FromBody] SkipCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SkipCheckInCommand(request.Type, request.CheckInDate);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
}
