using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.CheckIns.Models;

namespace Mastery.Application.Features.CheckIns.Queries.GetCheckInById;

/// <summary>
/// Gets a single check-in by ID with full details.
/// </summary>
public sealed record GetCheckInByIdQuery(Guid Id) : IQuery<CheckInDto>;
