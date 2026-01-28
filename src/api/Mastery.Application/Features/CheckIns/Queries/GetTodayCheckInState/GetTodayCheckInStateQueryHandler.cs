using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.CheckIns.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.CheckIns.Queries.GetTodayCheckInState;

public sealed class GetTodayCheckInStateQueryHandler : IQueryHandler<GetTodayCheckInStateQuery, TodayCheckInStateDto>
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetTodayCheckInStateQueryHandler(
        ICheckInRepository checkInRepository,
        ICurrentUserService currentUserService)
    {
        _checkInRepository = checkInRepository;
        _currentUserService = currentUserService;
    }

    public async Task<TodayCheckInStateDto> Handle(GetTodayCheckInStateQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todayCheckIns = await _checkInRepository.GetTodayStateAsync(userId, today, cancellationToken);

        var morning = todayCheckIns.FirstOrDefault(c => c.Type == CheckInType.Morning);
        var evening = todayCheckIns.FirstOrDefault(c => c.Type == CheckInType.Evening);

        var streak = await _checkInRepository.CalculateStreakAsync(userId, today, cancellationToken);

        return new TodayCheckInStateDto(
            MorningCheckIn: morning?.ToDto(),
            EveningCheckIn: evening?.ToDto(),
            MorningStatus: morning?.Status.ToString() ?? "Pending",
            EveningStatus: evening?.Status.ToString() ?? "Pending",
            CheckInStreakDays: streak);
    }
}
