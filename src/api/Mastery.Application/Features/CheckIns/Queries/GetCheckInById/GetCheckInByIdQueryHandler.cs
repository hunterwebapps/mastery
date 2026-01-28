using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.CheckIns.Models;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.CheckIns.Queries.GetCheckInById;

public sealed class GetCheckInByIdQueryHandler : IQueryHandler<GetCheckInByIdQuery, CheckInDto>
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCheckInByIdQueryHandler(
        ICheckInRepository checkInRepository,
        ICurrentUserService currentUserService)
    {
        _checkInRepository = checkInRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CheckInDto> Handle(GetCheckInByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var checkIn = await _checkInRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.CheckIn.CheckIn), request.Id);

        if (checkIn.UserId != userId)
            throw new DomainException("Access denied.");

        return checkIn.ToDto();
    }
}
