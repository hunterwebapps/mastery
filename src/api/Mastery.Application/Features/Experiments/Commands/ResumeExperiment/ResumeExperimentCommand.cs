using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.ResumeExperiment;

/// <summary>
/// Resumes a paused experiment.
/// Only one experiment can be active per user at a time.
/// </summary>
public sealed record ResumeExperimentCommand(Guid Id) : ICommand;
