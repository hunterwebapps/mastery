using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Projects.Models;

namespace Mastery.Application.Features.Projects.Queries.GetProjectById;

/// <summary>
/// Gets a project by ID with full details including milestones.
/// </summary>
public sealed record GetProjectByIdQuery(Guid Id) : IQuery<ProjectDto>;
