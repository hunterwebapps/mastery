using Mastery.Api.Contracts.Projects;
using Mastery.Application.Features.Projects.Commands.AddMilestone;
using Mastery.Application.Features.Projects.Commands.ChangeProjectStatus;
using Mastery.Application.Features.Projects.Commands.CompleteMilestone;
using Mastery.Application.Features.Projects.Commands.CompleteProject;
using Mastery.Application.Features.Projects.Commands.CreateProject;
using Mastery.Application.Features.Projects.Commands.RemoveMilestone;
using Mastery.Application.Features.Projects.Commands.SetProjectNextAction;
using Mastery.Application.Features.Projects.Commands.UpdateMilestone;
using Mastery.Application.Features.Projects.Models;
using Mastery.Application.Features.Projects.Queries.GetProjectById;
using Mastery.Application.Features.Projects.Queries.GetProjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages projects - execution containers for achieving goals.
/// </summary>
[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ISender _mediator;

    public ProjectsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all projects for the current user, optionally filtered.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string? status,
        [FromQuery] Guid? goalId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetProjectsQuery(status, goalId),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single project by ID with full details including milestones.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var milestones = request.Milestones?.Select(m => new CreateMilestoneInput(
            m.Title,
            m.TargetDate,
            m.Notes)).ToList();

        var command = new CreateProjectCommand(
            request.Title,
            request.Description,
            request.Priority,
            request.GoalId,
            request.SeasonId,
            request.TargetEndDate,
            request.RoleIds,
            request.ValueIds,
            milestones,
            request.SaveAsDraft);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProjectById), new { id }, id);
    }

    /// <summary>
    /// Changes the status of a project (activate, pause, archive).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeProjectStatus(
        Guid id,
        [FromBody] ChangeProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeProjectStatusCommand(id, request.NewStatus);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sets or clears the next action (task) for a project.
    /// </summary>
    [HttpPut("{id:guid}/next-action")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetProjectNextAction(
        Guid id,
        [FromBody] SetProjectNextActionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetProjectNextActionCommand(id, request.TaskId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Completes a project with optional outcome notes.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteProject(
        Guid id,
        [FromBody] CompleteProjectRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteProjectCommand(id, request?.OutcomeNotes);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Archives (soft-deletes) a project.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ChangeProjectStatusCommand(id, "Archived");
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    #region Milestones

    /// <summary>
    /// Adds a new milestone to a project.
    /// </summary>
    [HttpPost("{id:guid}/milestones")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMilestone(
        Guid id,
        [FromBody] AddMilestoneRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddMilestoneCommand(id, request.Title, request.TargetDate, request.Notes);
        var milestoneId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProjectById), new { id }, milestoneId);
    }

    /// <summary>
    /// Updates an existing milestone.
    /// </summary>
    [HttpPut("{id:guid}/milestones/{milestoneId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMilestone(
        Guid id,
        Guid milestoneId,
        [FromBody] UpdateMilestoneRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMilestoneCommand(
            id,
            milestoneId,
            request.Title,
            request.TargetDate,
            request.Notes,
            request.DisplayOrder);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Marks a milestone as completed.
    /// </summary>
    [HttpPost("{id:guid}/milestones/{milestoneId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteMilestone(
        Guid id,
        Guid milestoneId,
        CancellationToken cancellationToken)
    {
        var command = new CompleteMilestoneCommand(id, milestoneId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Removes a milestone from a project.
    /// </summary>
    [HttpDelete("{id:guid}/milestones/{milestoneId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMilestone(
        Guid id,
        Guid milestoneId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveMilestoneCommand(id, milestoneId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    #endregion
}
