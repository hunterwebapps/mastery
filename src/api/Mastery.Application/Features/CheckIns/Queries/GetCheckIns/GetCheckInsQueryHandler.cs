using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.CheckIns.Models;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.CheckIns.Queries.GetCheckIns;

public sealed class GetCheckInsQueryHandler : IQueryHandler<GetCheckInsQuery, List<CheckInSummaryDto>>
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCheckInsQueryHandler(
        ICheckInRepository checkInRepository,
        ICurrentUserService currentUserService)
    {
        _checkInRepository = checkInRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<CheckInSummaryDto>> Handle(GetCheckInsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var fromDate = !string.IsNullOrEmpty(request.FromDate) && DateOnly.TryParse(request.FromDate, out var parsedFrom)
            ? parsedFrom
            : today.AddDays(-7);

        var toDate = !string.IsNullOrEmpty(request.ToDate) && DateOnly.TryParse(request.ToDate, out var parsedTo)
            ? parsedTo
            : today;

        var checkIns = await _checkInRepository.GetByUserIdAndDateRangeAsync(
            userId, fromDate, toDate, cancellationToken);

        return checkIns.Select(c => c.ToSummaryDto()).ToList();
    }
}
