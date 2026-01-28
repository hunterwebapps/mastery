using Mastery.Api.Contracts.Tasks;
using Mastery.Application.Features.Tasks.Commands.ArchiveTask;
using Mastery.Application.Features.Tasks.Commands.CancelTask;
using Mastery.Application.Features.Tasks.Commands.CompleteTask;
using Mastery.Application.Features.Tasks.Commands.CreateTask;
using Mastery.Application.Features.Tasks.Commands.MoveTaskToReady;
using Mastery.Application.Features.Tasks.Commands.RescheduleTask;
using Mastery.Application.Features.Tasks.Commands.ScheduleTask;
using Mastery.Application.Features.Tasks.Commands.UndoTaskCompletion;
using Mastery.Application.Features.Tasks.Commands.UpdateTask;
using Mastery.Application.Features.Tasks.Models;
using Mastery.Application.Features.Tasks.Queries.GetInboxTasks;
using Mastery.Application.Features.Tasks.Queries.GetTaskById;
using Mastery.Application.Features.Tasks.Queries.GetTasks;
using Mastery.Application.Features.Tasks.Queries.GetTodayTasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

/// <summary>
/// Manages tasks - the primary "actuators" in the Mastery control loop.
/// </summary>
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ISender _mediator;

    public TasksController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all tasks for the current user, optionally filtered.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks(
        [FromQuery] string? status,
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? goalId,
        [FromQuery] string? contextTag,
        [FromQuery] bool? isOverdue,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetTasksQuery(status, projectId, goalId, contextTag, isOverdue),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets tasks for today's daily loop (scheduled today + due today + overdue).
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(IReadOnlyList<TodayTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodayTasks(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTodayTasksQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets tasks in Inbox status for capture/triage.
    /// </summary>
    [HttpGet("inbox")]
    [ProducesResponseType(typeof(IReadOnlyList<InboxTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInboxTasks(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInboxTasksQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single task by ID with all details.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTaskByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        CreateTaskDueInput? due = request.Due != null
            ? new CreateTaskDueInput(request.Due.DueOn, request.Due.DueAt, request.Due.DueType)
            : null;

        CreateTaskSchedulingInput? scheduling = request.Scheduling != null
            ? new CreateTaskSchedulingInput(
                request.Scheduling.ScheduledOn,
                request.Scheduling.PreferredTimeWindowStart,
                request.Scheduling.PreferredTimeWindowEnd)
            : null;

        var bindings = request.MetricBindings?.Select(b => new CreateTaskMetricBindingInput(
            b.MetricDefinitionId,
            b.ContributionType,
            b.FixedValue,
            b.Notes)).ToList();

        var command = new CreateTaskCommand(
            request.Title,
            request.Description,
            request.EstimatedMinutes,
            request.EnergyCost,
            request.Priority,
            request.ProjectId,
            request.GoalId,
            due,
            scheduling,
            request.ContextTags,
            request.DependencyTaskIds,
            request.RoleIds,
            request.ValueIds,
            bindings,
            request.StartAsReady);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTaskById), new { id }, id);
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        UpdateTaskDueInput? due = request.Due != null
            ? new UpdateTaskDueInput(request.Due.DueOn, request.Due.DueAt, request.Due.DueType)
            : null;

        var command = new UpdateTaskCommand(
            id,
            request.Title,
            request.Description,
            request.EstimatedMinutes,
            request.EnergyCost,
            request.Priority,
            request.ProjectId,
            request.GoalId,
            due,
            ClearDue: request.Due == null,
            request.ContextTags,
            request.RoleIds,
            request.ValueIds);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Schedules a task for a specific date.
    /// </summary>
    [HttpPost("{id:guid}/schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScheduleTask(
        Guid id,
        [FromBody] ScheduleTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ScheduleTaskCommand(
            id,
            request.ScheduledOn,
            request.PreferredTimeWindowStart,
            request.PreferredTimeWindowEnd);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reschedules a task to a new date.
    /// </summary>
    [HttpPost("{id:guid}/reschedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RescheduleTask(
        Guid id,
        [FromBody] RescheduleTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RescheduleTaskCommand(id, request.NewDate, request.Reason);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Completes a task.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteTask(
        Guid id,
        [FromBody] CompleteTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteTaskCommand(
            id,
            request.CompletedOn,
            request.ActualMinutes,
            request.Note,
            request.EnteredValue);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Undoes a task completion (returns to Ready status).
    /// </summary>
    [HttpPost("{id:guid}/undo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UndoTaskCompletion(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new UndoTaskCompletionCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cancels a task.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTask(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new CancelTaskCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Moves a task from Inbox to Ready status.
    /// </summary>
    [HttpPost("{id:guid}/ready")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveTaskToReady(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new MoveTaskToReadyCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Archives (soft-deletes) a task.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ArchiveTaskCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
