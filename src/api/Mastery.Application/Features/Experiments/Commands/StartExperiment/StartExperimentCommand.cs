using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.StartExperiment;

/// <summary>
/// Starts an experiment. Transitions from Draft to Active.
/// Only one experiment can be active per user at a time.
/// </summary>
public sealed record StartExperimentCommand(Guid Id) : ICommand;
