using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Application.Features.Users.Models;

namespace Mastery.Application.Features.Recommendations.Queries.GetAdminTraces;

public sealed record GetAdminTracesQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Context = null,
    string? Status = null,
    string? UserId = null,
    string? SelectionMethod = null,
    int? FinalTier = null,
    int Page = 1,
    int PageSize = 20
) : IQuery<PaginatedList<AdminTraceListDto>>;
