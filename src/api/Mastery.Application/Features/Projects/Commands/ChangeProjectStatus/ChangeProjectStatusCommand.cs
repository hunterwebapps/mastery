using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.ChangeProjectStatus;

/// <summary>
/// Changes the status of a project.
/// </summary>
public sealed record ChangeProjectStatusCommand(
    Guid ProjectId,
    string NewStatus) : ICommand;
