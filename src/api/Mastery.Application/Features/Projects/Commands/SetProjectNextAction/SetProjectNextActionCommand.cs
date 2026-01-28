using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.SetProjectNextAction;

/// <summary>
/// Sets the next task for a project.
/// </summary>
public sealed record SetProjectNextActionCommand(
    Guid ProjectId,
    Guid? TaskId) : ICommand;
