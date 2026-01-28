using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.CheckIns.Commands.SkipCheckIn;

public sealed class SkipCheckInCommandHandler : ICommandHandler<SkipCheckInCommand, Guid>
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SkipCheckInCommandHandler(
        ICheckInRepository checkInRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _checkInRepository = checkInRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SkipCheckInCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        if (!Enum.TryParse<CheckInType>(request.Type, out var type))
            throw new DomainException($"Invalid check-in type: {request.Type}");

        var checkInDate = !string.IsNullOrEmpty(request.CheckInDate) && DateOnly.TryParse(request.CheckInDate, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // Enforce uniqueness
        var exists = await _checkInRepository.ExistsByUserIdAndDateAndTypeAsync(
            userId, checkInDate, type, cancellationToken);

        if (exists)
            throw new DomainException($"A {type.ToString().ToLowerInvariant()} check-in already exists for {checkInDate:yyyy-MM-dd}.");

        var checkIn = CheckIn.CreateSkipped(userId, checkInDate, type);

        await _checkInRepository.AddAsync(checkIn, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return checkIn.Id;
    }
}
