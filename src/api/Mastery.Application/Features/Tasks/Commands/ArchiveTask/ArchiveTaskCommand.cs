using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Tasks.Commands.ArchiveTask;

/// <summary>
/// Archives a task (soft delete).
/// </summary>
public sealed record ArchiveTaskCommand(Guid TaskId) : ICommand;
