using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.CheckIns.Models;

namespace Mastery.Application.Features.CheckIns.Queries.GetTodayCheckInState;

/// <summary>
/// Gets today's check-in state for the daily loop view.
/// </summary>
public sealed record GetTodayCheckInStateQuery : IQuery<TodayCheckInStateDto>;
