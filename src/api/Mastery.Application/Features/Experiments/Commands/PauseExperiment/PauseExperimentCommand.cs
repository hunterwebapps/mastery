using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.PauseExperiment;

/// <summary>
/// Pauses an active experiment.
/// </summary>
public sealed record PauseExperimentCommand(Guid Id) : ICommand;
