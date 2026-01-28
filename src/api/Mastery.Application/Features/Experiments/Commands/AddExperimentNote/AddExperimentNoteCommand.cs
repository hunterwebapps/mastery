using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.AddExperimentNote;

/// <summary>
/// Adds a note to an experiment.
/// </summary>
public sealed record AddExperimentNoteCommand(
    Guid ExperimentId,
    string Content) : ICommand<Guid>;
