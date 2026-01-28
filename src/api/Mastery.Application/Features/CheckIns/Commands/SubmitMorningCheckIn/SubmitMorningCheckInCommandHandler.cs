using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.CheckIns.Commands.SubmitMorningCheckIn;

public sealed class SubmitMorningCheckInCommandHandler : ICommandHandler<SubmitMorningCheckInCommand, Guid>
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitMorningCheckInCommandHandler(
        ICheckInRepository checkInRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _checkInRepository = checkInRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SubmitMorningCheckInCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Parse check-in date (defaults to today)
        var checkInDate = !string.IsNullOrEmpty(request.CheckInDate) && DateOnly.TryParse(request.CheckInDate, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // Enforce uniqueness: one morning check-in per user per date
        var exists = await _checkInRepository.ExistsByUserIdAndDateAndTypeAsync(
            userId, checkInDate, CheckInType.Morning, cancellationToken);

        if (exists)
            throw new DomainException($"A morning check-in already exists for {checkInDate:yyyy-MM-dd}.");

        // Parse enums
        if (!Enum.TryParse<HabitMode>(request.SelectedMode, out var selectedMode))
            throw new DomainException($"Invalid mode: {request.SelectedMode}");

        Top1Type? top1Type = null;
        if (!string.IsNullOrEmpty(request.Top1Type))
        {
            if (!Enum.TryParse<Top1Type>(request.Top1Type, out var parsedTop1Type))
                throw new DomainException($"Invalid Top 1 type: {request.Top1Type}");
            top1Type = parsedTop1Type;
        }

        var checkIn = CheckIn.CreateMorning(
            userId: userId,
            checkInDate: checkInDate,
            energyLevel: request.EnergyLevel,
            selectedMode: selectedMode,
            top1Type: top1Type,
            top1EntityId: request.Top1EntityId,
            top1FreeText: request.Top1FreeText,
            intention: request.Intention);

        await _checkInRepository.AddAsync(checkIn, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return checkIn.Id;
    }
}
