using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Application.Features.Users.Models;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Queries.GetAdminTraces;

public sealed class GetAdminTracesQueryHandler(
    IRecommendationRepository _recommendationRepository,
    IUserManagementService _userManagementService)
    : IQueryHandler<GetAdminTracesQuery, PaginatedList<AdminTraceListDto>>
{
    public async Task<PaginatedList<AdminTraceListDto>> Handle(
        GetAdminTracesQuery request,
        CancellationToken cancellationToken)
    {
        // Get traces from repository
        var (items, totalCount) = await _recommendationRepository.GetAdminTracesAsync(
            request.DateFrom,
            request.DateTo,
            request.Context,
            request.Status,
            request.UserId,
            request.SelectionMethod,
            request.FinalTier,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Get agent run stats and user emails in batches
        var traceIds = items.Select(x => x.Trace.Id).ToList();
        var userIds = items.Select(x => x.Recommendation.UserId).Distinct().ToList();

        // Get agent run stats
        var agentRunStats = await _recommendationRepository.GetAgentRunStatsByTraceIdsAsync(traceIds, cancellationToken);

        // Get user emails
        var userEmails = new Dictionary<string, string>();
        foreach (var userId in userIds)
        {
            var user = await _userManagementService.GetUserByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                userEmails[userId] = user.Email;
            }
        }

        // Map to DTOs
        var dtos = items.Select(x => new AdminTraceListDto
        {
            Id = x.Trace.Id,
            RecommendationId = x.Trace.RecommendationId,
            UserId = x.Recommendation.UserId,
            UserEmail = userEmails.GetValueOrDefault(x.Recommendation.UserId, x.Recommendation.UserId),
            RecommendationType = x.Recommendation.Type.ToString(),
            RecommendationStatus = x.Recommendation.Status.ToString(),
            Context = x.Recommendation.Context.ToString(),
            SelectionMethod = x.Trace.SelectionMethod,
            FinalTier = x.Trace.FinalTier,
            ProcessingWindowType = x.Trace.ProcessingWindowType,
            TotalDurationMs = x.Trace.TotalDurationMs,
            TotalTokens = agentRunStats.GetValueOrDefault(x.Trace.Id).TotalTokens,
            AgentRunCount = agentRunStats.GetValueOrDefault(x.Trace.Id).Count,
            CreatedAt = x.Trace.CreatedAt
        }).ToList();

        return new PaginatedList<AdminTraceListDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
