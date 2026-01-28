using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Projects.Models;

namespace Mastery.Application.Features.Projects.Queries.GetProjects;

/// <summary>
/// Gets projects with optional filtering.
/// </summary>
public sealed record GetProjectsQuery(
    string? Status = null,
    Guid? GoalId = null) : IQuery<List<ProjectSummaryDto>>;
