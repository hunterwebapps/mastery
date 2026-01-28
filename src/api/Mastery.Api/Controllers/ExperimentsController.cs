using Mastery.Api.Contracts.Experiments;
using Mastery.Application.Features.Experiments.Commands.AbandonExperiment;
using Mastery.Application.Features.Experiments.Commands.AddExperimentNote;
using Mastery.Application.Features.Experiments.Commands.CompleteExperiment;
using Mastery.Application.Features.Experiments.Commands.CreateExperiment;
using Mastery.Application.Features.Experiments.Commands.PauseExperiment;
using Mastery.Application.Features.Experiments.Commands.ResumeExperiment;
using Mastery.Application.Features.Experiments.Commands.StartExperiment;
using Mastery.Application.Features.Experiments.Commands.UpdateExperiment;
using Mastery.Application.Features.Experiments.Models;
using Mastery.Application.Features.Experiments.Queries.GetActiveExperiment;
using Mastery.Application.Features.Experiments.Queries.GetExperimentById;
using Mastery.Application.Features.Experiments.Queries.GetExperiments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages experiments for testing hypotheses about personal development changes.
/// </summary>
[ApiController]
[Route("api/experiments")]
public class ExperimentsController : ControllerBase
{
    private readonly ISender _mediator;

    public ExperimentsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all experiments for the current user, optionally filtered by status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ExperimentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExperiments(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExperimentsQuery(status), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single experiment by ID with all details.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExperimentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExperimentById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExperimentByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the currently active experiment for the user, if any.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ExperimentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActiveExperiment(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveExperimentQuery(), cancellationToken);
        return result != null ? Ok(result) : NoContent();
    }

    /// <summary>
    /// Creates a new experiment.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExperiment(
        [FromBody] CreateExperimentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateExperimentCommand(
            request.Title,
            request.Category,
            request.CreatedFrom,
            new CreateHypothesisInput(
                request.Hypothesis.Change,
                request.Hypothesis.ExpectedOutcome,
                request.Hypothesis.Rationale),
            new CreateMeasurementPlanInput(
                request.MeasurementPlan.PrimaryMetricDefinitionId,
                request.MeasurementPlan.PrimaryAggregation,
                request.MeasurementPlan.BaselineWindowDays,
                request.MeasurementPlan.RunWindowDays,
                request.MeasurementPlan.GuardrailMetricDefinitionIds,
                request.MeasurementPlan.MinComplianceThreshold),
            request.Description,
            request.LinkedGoalIds,
            request.StartDate,
            request.EndDatePlanned);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetExperimentById), new { id }, id);
    }

    /// <summary>
    /// Updates an experiment (draft only).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExperiment(
        Guid id,
        [FromBody] UpdateExperimentRequest request,
        CancellationToken cancellationToken)
    {
        var hypothesis = request.Hypothesis != null
            ? new CreateHypothesisInput(
                request.Hypothesis.Change,
                request.Hypothesis.ExpectedOutcome,
                request.Hypothesis.Rationale)
            : null;

        var measurementPlan = request.MeasurementPlan != null
            ? new CreateMeasurementPlanInput(
                request.MeasurementPlan.PrimaryMetricDefinitionId,
                request.MeasurementPlan.PrimaryAggregation,
                request.MeasurementPlan.BaselineWindowDays,
                request.MeasurementPlan.RunWindowDays,
                request.MeasurementPlan.GuardrailMetricDefinitionIds,
                request.MeasurementPlan.MinComplianceThreshold)
            : null;

        var command = new UpdateExperimentCommand(
            id,
            request.Title,
            request.Description,
            request.Category,
            hypothesis,
            measurementPlan,
            request.LinkedGoalIds,
            request.StartDate,
            request.EndDatePlanned);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Starts an experiment (draft → active).
    /// </summary>
    [HttpPut("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartExperiment(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new StartExperimentCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Pauses an experiment (active → paused).
    /// </summary>
    [HttpPut("{id:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseExperiment(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new PauseExperimentCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Resumes a paused experiment (paused → active).
    /// </summary>
    [HttpPut("{id:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeExperiment(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResumeExperimentCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Completes an experiment with results (active/paused → completed).
    /// </summary>
    [HttpPut("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteExperiment(
        Guid id,
        [FromBody] CompleteExperimentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteExperimentCommand(
            id,
            request.OutcomeClassification,
            request.BaselineValue,
            request.RunValue,
            request.ComplianceRate,
            request.NarrativeSummary);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Abandons an experiment (active/paused → abandoned).
    /// </summary>
    [HttpPut("{id:guid}/abandon")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AbandonExperiment(
        Guid id,
        [FromBody] AbandonExperimentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AbandonExperimentCommand(id, request.Reason);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Adds a note to an experiment.
    /// </summary>
    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] AddExperimentNoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddExperimentNoteCommand(id, request.Content);
        var noteId = await _mediator.Send(command, cancellationToken);
        return Created($"api/experiments/{id}/notes/{noteId}", noteId);
    }
}
