using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.CheckIns.Models;

namespace Mastery.Application.Features.CheckIns.Queries.GetCheckIns;

/// <summary>
/// Gets check-in history for a date range.
/// </summary>
public sealed record GetCheckInsQuery(
    string? FromDate = null,
    string? ToDate = null) : IQuery<List<CheckInSummaryDto>>;
