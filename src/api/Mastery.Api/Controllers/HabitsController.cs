using Mastery.Api.Contracts.Habits;
using Mastery.Application.Features.Habits.Commands.CompleteOccurrence;
using Mastery.Application.Features.Habits.Commands.CreateHabit;
using Mastery.Application.Features.Habits.Commands.SkipOccurrence;
using Mastery.Application.Features.Habits.Commands.UndoOccurrence;
using Mastery.Application.Features.Habits.Commands.UpdateHabit;
using Mastery.Application.Features.Habits.Commands.UpdateHabitStatus;
using Mastery.Application.Features.Habits.Models;
using Mastery.Application.Features.Habits.Queries.GetHabitById;
using Mastery.Application.Features.Habits.Queries.GetHabitHistory;
using Mastery.Application.Features.Habits.Queries.GetHabits;
using Mastery.Application.Features.Habits.Queries.GetTodayHabits;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages habits and their occurrences.
/// </summary>
[ApiController]
[Route("api/habits")]
public class HabitsController : ControllerBase
{
    private readonly ISender _mediator;

    public HabitsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all habits for the current user, optionally filtered by status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HabitSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHabits(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHabitsQuery(status), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets habits for today's daily loop (optimized for one-tap completion).
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(IReadOnlyList<TodayHabitDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodayHabits(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTodayHabitsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single habit by ID with all details.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HabitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHabitById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHabitByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets habit history (occurrences) within a date range.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(HabitHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHabitHistory(
        Guid id,
        [FromQuery] string fromDate,
        [FromQuery] string toDate,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetHabitHistoryQuery(id, fromDate, toDate),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new habit.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateHabit(
        [FromBody] CreateHabitRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = new CreateHabitScheduleInput(
            request.Schedule.Type,
            request.Schedule.DaysOfWeek,
            request.Schedule.PreferredTimes,
            request.Schedule.FrequencyPerWeek,
            request.Schedule.IntervalDays,
            request.Schedule.StartDate,
            request.Schedule.EndDate);

        CreateHabitPolicyInput? policy = request.Policy != null
            ? new CreateHabitPolicyInput(
                request.Policy.AllowLateCompletion,
                request.Policy.LateCutoffTime,
                request.Policy.AllowSkip,
                request.Policy.RequireMissReason,
                request.Policy.AllowBackfill,
                request.Policy.MaxBackfillDays)
            : null;

        var bindings = request.MetricBindings?.Select(b => new CreateHabitMetricBindingInput(
            b.MetricDefinitionId,
            b.ContributionType,
            b.FixedValue,
            b.Notes)).ToList();

        var variants = request.Variants?.Select(v => new CreateHabitVariantInput(
            v.Mode,
            v.Label,
            v.DefaultValue,
            v.EstimatedMinutes,
            v.EnergyCost,
            v.CountsAsCompletion)).ToList();

        var command = new CreateHabitCommand(
            request.Title,
            schedule,
            request.Description,
            request.Why,
            policy,
            request.DefaultMode,
            bindings,
            variants,
            request.RoleIds,
            request.ValueIds,
            request.GoalIds);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetHabitById), new { id }, id);
    }

    /// <summary>
    /// Updates a habit's properties.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHabit(
        Guid id,
        [FromBody] UpdateHabitRequest request,
        CancellationToken cancellationToken)
    {
        CreateHabitScheduleInput? schedule = request.Schedule != null
            ? new CreateHabitScheduleInput(
                request.Schedule.Type,
                request.Schedule.DaysOfWeek,
                request.Schedule.PreferredTimes,
                request.Schedule.FrequencyPerWeek,
                request.Schedule.IntervalDays,
                request.Schedule.StartDate,
                request.Schedule.EndDate)
            : null;

        CreateHabitPolicyInput? policy = request.Policy != null
            ? new CreateHabitPolicyInput(
                request.Policy.AllowLateCompletion,
                request.Policy.LateCutoffTime,
                request.Policy.AllowSkip,
                request.Policy.RequireMissReason,
                request.Policy.AllowBackfill,
                request.Policy.MaxBackfillDays)
            : null;

        var command = new UpdateHabitCommand(
            id,
            request.Title,
            request.Description,
            request.Why,
            request.DefaultMode,
            schedule,
            policy,
            request.RoleIds,
            request.ValueIds,
            request.GoalIds);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Updates a habit's status (activate, pause, archive).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHabitStatus(
        Guid id,
        [FromBody] UpdateHabitStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateHabitStatusCommand(id, request.NewStatus);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Archives (soft-deletes) a habit.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHabit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new UpdateHabitStatusCommand(id, "Archived");
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Completes a habit occurrence for a specific date.
    /// </summary>
    [HttpPost("{id:guid}/occurrences/{date}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteOccurrence(
        Guid id,
        string date,
        [FromBody] CompleteOccurrenceRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteOccurrenceCommand(
            id,
            date,
            request?.Mode,
            request?.Value,
            request?.Note);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Undoes a completed habit occurrence.
    /// </summary>
    [HttpPost("{id:guid}/occurrences/{date}/undo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UndoOccurrence(
        Guid id,
        string date,
        CancellationToken cancellationToken)
    {
        var command = new UndoOccurrenceCommand(id, date);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Skips a habit occurrence.
    /// </summary>
    [HttpPost("{id:guid}/occurrences/{date}/skip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SkipOccurrence(
        Guid id,
        string date,
        [FromBody] SkipOccurrenceRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new SkipOccurrenceCommand(id, date, request?.Reason);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
