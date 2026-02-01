using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;

namespace Mastery.Application.Features.Recommendations.Queries.GetAdminTraceById;

public sealed record GetAdminTraceByIdQuery(Guid TraceId) : IQuery<AdminTraceDetailDto?>;
