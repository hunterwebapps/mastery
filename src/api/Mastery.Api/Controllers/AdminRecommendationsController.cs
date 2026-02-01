using Mastery.Application.Features.Recommendations.Models;
using Mastery.Application.Features.Recommendations.Queries.GetAdminTraces;
using Mastery.Application.Features.Recommendations.Queries.GetAdminTraceById;
using Mastery.Application.Features.Users.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Admin endpoints for debugging recommendation pipeline traces.
/// </summary>
[ApiController]
[Route("api/admin/recommendations")]
[Authorize(Policy = "RequireAdmin")]
public class AdminRecommendationsController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminRecommendationsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets paginated list of recommendation traces with filtering.
    /// </summary>
    [HttpGet("traces")]
    [ProducesResponseType(typeof(PaginatedList<AdminTraceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTraces(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? context,
        [FromQuery] string? status,
        [FromQuery] string? userId,
        [FromQuery] string? selectionMethod,
        [FromQuery] int? finalTier,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAdminTracesQuery(
                DateFrom: dateFrom,
                DateTo: dateTo,
                Context: context,
                Status: status,
                UserId: userId,
                SelectionMethod: selectionMethod,
                FinalTier: finalTier,
                Page: page,
                PageSize: pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets detailed trace information including decompressed state snapshot and agent runs.
    /// </summary>
    [HttpGet("traces/{id:guid}")]
    [ProducesResponseType(typeof(AdminTraceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTraceById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAdminTraceByIdQuery(id),
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
