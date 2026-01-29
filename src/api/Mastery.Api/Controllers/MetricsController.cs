using Mastery.Api.Contracts.Metrics;
using Mastery.Application.Features.Metrics.Commands.CreateMetricDefinition;
using Mastery.Application.Features.Metrics.Commands.RecordObservation;
using Mastery.Application.Features.Metrics.Commands.UpdateMetricDefinition;
using Mastery.Application.Features.Metrics.Models;
using Mastery.Application.Features.Metrics.Queries.GetMetricDefinitions;
using Mastery.Application.Features.Metrics.Queries.GetObservations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages metric definitions and observations.
/// </summary>
[ApiController]
[Route("api/metrics")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly ISender _mediator;

    public MetricsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all metric definitions for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MetricDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetricDefinitions(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetMetricDefinitionsQuery(includeArchived), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new metric definition.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMetricDefinition(
        [FromBody] CreateMetricDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var unit = request.Unit is not null
            ? new CreateMetricUnitInput(request.Unit.Type, request.Unit.Label)
            : null;

        var command = new CreateMetricDefinitionCommand(
            request.Name,
            request.Description,
            request.DataType,
            unit,
            request.Direction,
            request.DefaultCadence,
            request.DefaultAggregation,
            request.Tags);

        var id = await _mediator.Send(command, cancellationToken);
        return Created($"/api/metrics/{id}", id);
    }

    /// <summary>
    /// Updates a metric definition.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMetricDefinition(
        Guid id,
        [FromBody] UpdateMetricDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var unit = request.Unit is not null
            ? new CreateMetricUnitInput(request.Unit.Type, request.Unit.Label)
            : null;

        var command = new UpdateMetricDefinitionCommand(
            id,
            request.Name,
            request.Description,
            request.DataType,
            unit,
            request.Direction,
            request.DefaultCadence,
            request.DefaultAggregation,
            request.IsArchived,
            request.Tags);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Records a new observation for a metric.
    /// </summary>
    [HttpPost("{id:guid}/observations")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordObservation(
        Guid id,
        [FromBody] RecordObservationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordObservationCommand(
            id,
            request.Value,
            request.ObservedOn,
            request.Source,
            request.CorrelationId,
            request.Note);

        var observationId = await _mediator.Send(command, cancellationToken);
        return Created($"/api/metrics/{id}/observations/{observationId}", observationId);
    }

    /// <summary>
    /// Gets observations for a metric within a date range.
    /// </summary>
    [HttpGet("{id:guid}/observations")]
    [ProducesResponseType(typeof(MetricTimeSeriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetObservations(
        Guid id,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetObservationsQuery(id, startDate, endDate), cancellationToken);
        return Ok(result);
    }
}
