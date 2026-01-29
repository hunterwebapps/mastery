using Mastery.Api.Contracts.Recommendations;
using Mastery.Application.Features.Recommendations.Commands.AcceptRecommendation;
using Mastery.Application.Features.Recommendations.Commands.DismissRecommendation;
using Mastery.Application.Features.Recommendations.Commands.GenerateRecommendations;
using Mastery.Application.Features.Recommendations.Commands.SnoozeRecommendation;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Application.Features.Recommendations.Queries.GetActiveRecommendations;
using Mastery.Application.Features.Recommendations.Queries.GetRecommendationById;
using Mastery.Application.Features.Recommendations.Queries.GetRecommendationHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ExecutionResult = Mastery.Application.Features.Recommendations.Models.ExecutionResult;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages AI-generated recommendations and their lifecycle.
/// </summary>
[ApiController]
[Route("api/recommendations")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly ISender _mediator;

    public RecommendationsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Triggers the recommendation pipeline for a given context.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateRecommendations(
        [FromBody] GenerateRecommendationsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateRecommendationsCommand(request.Context);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets active (pending/snoozed) recommendations, optionally filtered by context.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveRecommendations(
        [FromQuery] string? context,
        CancellationToken cancellationToken)
    {
        var query = new GetActiveRecommendationsQuery(context);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single recommendation by ID with full trace details.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RecommendationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommendationById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRecommendationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Accepts and executes a recommendation.
    /// Returns execution result with redirect path for the created/modified entity.
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(ExecutionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptRecommendation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcceptRecommendationCommand(id), cancellationToken);
        return Ok(ToDto(result));
    }

    private static ExecutionResultDto ToDto(ExecutionResult result) =>
        new(
            EntityId: result.EntityId?.ToString(),
            EntityKind: result.EntityKind,
            Success: result.Success,
            ErrorMessage: result.ErrorMessage,
            ActionPayload: result.ActionPayload,
            ActionKind: result.ActionKind,
            TargetKind: result.TargetKind,
            TargetEntityId: result.TargetEntityId?.ToString(),
            RequiresClientAction: result.RequiresClientAction);

    /// <summary>
    /// Dismisses a recommendation with optional reason.
    /// </summary>
    [HttpPost("{id:guid}/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissRecommendation(
        Guid id,
        [FromBody] DismissRecommendationRequest? request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DismissRecommendationCommand(id, request?.Reason), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Snoozes a recommendation for later.
    /// </summary>
    [HttpPost("{id:guid}/snooze")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SnoozeRecommendation(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SnoozeRecommendationCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets recommendation history within an optional date range.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendationHistory(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = new GetRecommendationHistoryQuery(fromDate, toDate);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
