using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.AbandonExperiment;

/// <summary>
/// Abandons an active or paused experiment.
/// </summary>
public sealed record AbandonExperimentCommand(
    Guid Id,
    string? Reason = null) : ICommand;
