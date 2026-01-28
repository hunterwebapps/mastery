using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Goals.Models;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Goals.Queries.GetGoals;

public sealed class GetGoalsQueryHandler : IQueryHandler<GetGoalsQuery, IReadOnlyList<GoalSummaryDto>>
{
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetGoalsQueryHandler(
        IGoalRepository goalRepository,
        ICurrentUserService currentUserService)
    {
        _goalRepository = goalRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<GoalSummaryDto>> Handle(GetGoalsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return [];

        IReadOnlyList<Goal> goals;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<GoalStatus>(request.Status, out var status))
        {
            goals = await _goalRepository.GetByUserIdAndStatusAsync(userId, status, cancellationToken);
        }
        else
        {
            // Exclude archived goals by default when no status filter is provided
            var allGoals = await _goalRepository.GetByUserIdAsync(userId, cancellationToken);
            goals = allGoals.Where(g => g.Status != GoalStatus.Archived).ToList();
        }

        return goals.Select(MapToSummaryDto).ToList();
    }

    private static GoalSummaryDto MapToSummaryDto(Goal goal)
    {
        return new GoalSummaryDto
        {
            Id = goal.Id,
            Title = goal.Title,
            Status = goal.Status.ToString(),
            Priority = goal.Priority,
            Deadline = goal.Deadline,
            SeasonId = goal.SeasonId,
            MetricCount = goal.Metrics.Count,
            LagMetricCount = goal.Metrics.Count(m => m.Kind == MetricKind.Lag),
            LeadMetricCount = goal.Metrics.Count(m => m.Kind == MetricKind.Lead),
            ConstraintMetricCount = goal.Metrics.Count(m => m.Kind == MetricKind.Constraint),
            CreatedAt = goal.CreatedAt
        };
    }
}
