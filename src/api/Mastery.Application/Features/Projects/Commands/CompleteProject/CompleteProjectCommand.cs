using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.CompleteProject;

/// <summary>
/// Completes a project with optional outcome notes.
/// </summary>
public sealed record CompleteProjectCommand(
    Guid ProjectId,
    string? OutcomeNotes = null) : ICommand;
