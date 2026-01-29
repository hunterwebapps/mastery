using Mastery.Api.Contracts.Goals;
using Mastery.Application.Features.Goals.Commands.CreateGoal;
using Mastery.Application.Features.Goals.Commands.DeleteGoal;
using Mastery.Application.Features.Goals.Commands.UpdateGoal;
using Mastery.Application.Features.Goals.Commands.UpdateGoalScoreboard;
using Mastery.Application.Features.Goals.Commands.UpdateGoalStatus;
using Mastery.Application.Features.Goals.Models;
using Mastery.Application.Features.Goals.Queries.GetGoalById;
using Mastery.Application.Features.Goals.Queries.GetGoals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages goals and their scoreboards.
/// </summary>
[ApiController]
[Route("api/goals")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly ISender _mediator;

    public GoalsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all goals for the current user, optionally filtered by status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GoalSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoals(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGoalsQuery(status), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single goal by ID with all metrics.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGoalById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGoalByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new goal.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGoal(
        [FromBody] CreateGoalRequest request,
        CancellationToken cancellationToken)
    {
        var metrics = request.Metrics?.Select(m => new CreateGoalMetricInput(
            m.MetricDefinitionId,
            m.Kind,
            new CreateTargetInput(m.Target.Type, m.Target.Value, m.Target.MaxValue),
            new CreateEvaluationWindowInput(m.EvaluationWindow.WindowType, m.EvaluationWindow.RollingDays, m.EvaluationWindow.StartDay),
            m.Aggregation,
            m.SourceHint,
            m.Weight,
            m.DisplayOrder,
            m.Baseline,
            m.MinimumThreshold)).ToList();

        var command = new CreateGoalCommand(
            request.Title,
            request.Description,
            request.Why,
            request.Priority,
            request.Deadline,
            request.SeasonId,
            request.RoleIds,
            request.ValueIds,
            request.DependencyIds,
            metrics);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetGoalById), new { id }, id);
    }

    /// <summary>
    /// Updates a goal's basic properties.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGoal(
        Guid id,
        [FromBody] UpdateGoalRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGoalCommand(
            id,
            request.Title,
            request.Description,
            request.Why,
            request.Priority,
            request.Deadline,
            request.SeasonId,
            request.RoleIds,
            request.ValueIds,
            request.DependencyIds);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Updates a goal's status (activate, pause, complete, archive).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGoalStatus(
        Guid id,
        [FromBody] UpdateGoalStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGoalStatusCommand(id, request.NewStatus, request.CompletionNotes);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Updates a goal's scoreboard (metrics).
    /// </summary>
    [HttpPut("{id:guid}/scoreboard")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGoalScoreboard(
        Guid id,
        [FromBody] UpdateGoalScoreboardRequest request,
        CancellationToken cancellationToken)
    {
        var metrics = request.Metrics.Select(m => new UpdateGoalMetricInput(
            m.Id,
            m.MetricDefinitionId,
            m.Kind,
            new CreateTargetInput(m.Target.Type, m.Target.Value, m.Target.MaxValue),
            new CreateEvaluationWindowInput(m.EvaluationWindow.WindowType, m.EvaluationWindow.RollingDays, m.EvaluationWindow.StartDay),
            m.Aggregation,
            m.SourceHint,
            m.Weight,
            m.DisplayOrder,
            m.Baseline,
            m.MinimumThreshold)).ToList();

        var command = new UpdateGoalScoreboardCommand(id, metrics);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Archives (soft-deletes) a goal.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGoal(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteGoalCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
