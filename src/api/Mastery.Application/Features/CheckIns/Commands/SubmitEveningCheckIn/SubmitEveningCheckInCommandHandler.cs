using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.CheckIns.Commands.SubmitEveningCheckIn;

public sealed class SubmitEveningCheckInCommandHandler : ICommandHandler<SubmitEveningCheckInCommand, Guid>
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitEveningCheckInCommandHandler(
        ICheckInRepository checkInRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _checkInRepository = checkInRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SubmitEveningCheckInCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Parse check-in date (defaults to today)
        var checkInDate = !string.IsNullOrEmpty(request.CheckInDate) && DateOnly.TryParse(request.CheckInDate, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // Enforce uniqueness: one evening check-in per user per date
        var exists = await _checkInRepository.ExistsByUserIdAndDateAndTypeAsync(
            userId, checkInDate, CheckInType.Evening, cancellationToken);

        if (exists)
            throw new DomainException($"An evening check-in already exists for {checkInDate:yyyy-MM-dd}.");

        // Parse blocker category
        BlockerCategory? blockerCategory = null;
        if (!string.IsNullOrEmpty(request.BlockerCategory))
        {
            if (!Enum.TryParse<BlockerCategory>(request.BlockerCategory, out var parsedCategory))
                throw new DomainException($"Invalid blocker category: {request.BlockerCategory}");
            blockerCategory = parsedCategory;
        }

        var checkIn = CheckIn.CreateEvening(
            userId: userId,
            checkInDate: checkInDate,
            top1Completed: request.Top1Completed,
            energyLevelPm: request.EnergyLevelPm,
            stressLevel: request.StressLevel,
            reflection: request.Reflection,
            blockerCategory: blockerCategory,
            blockerNote: request.BlockerNote);

        await _checkInRepository.AddAsync(checkIn, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return checkIn.Id;
    }
}
