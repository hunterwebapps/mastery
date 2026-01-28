using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.CheckIns.Commands.UpdateCheckIn;

public sealed class UpdateCheckInCommandHandler : ICommandHandler<UpdateCheckInCommand>
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCheckInCommandHandler(
        ICheckInRepository checkInRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _checkInRepository = checkInRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async System.Threading.Tasks.Task Handle(UpdateCheckInCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var checkIn = await _checkInRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.CheckIn.CheckIn), request.Id);

        if (checkIn.UserId != userId)
            throw new DomainException("Access denied.");

        if (checkIn.Type == CheckInType.Morning)
        {
            HabitMode? mode = null;
            if (!string.IsNullOrEmpty(request.SelectedMode))
            {
                if (!Enum.TryParse<HabitMode>(request.SelectedMode, out var parsedMode))
                    throw new DomainException($"Invalid mode: {request.SelectedMode}");
                mode = parsedMode;
            }

            Top1Type? top1Type = null;
            if (!string.IsNullOrEmpty(request.Top1Type))
            {
                if (!Enum.TryParse<Top1Type>(request.Top1Type, out var parsedType))
                    throw new DomainException($"Invalid Top 1 type: {request.Top1Type}");
                top1Type = parsedType;
            }

            checkIn.UpdateMorning(
                energyLevel: request.EnergyLevel,
                selectedMode: mode,
                top1Type: top1Type,
                top1EntityId: request.Top1EntityId,
                top1FreeText: request.Top1FreeText,
                intention: request.Intention);
        }
        else
        {
            BlockerCategory? blockerCategory = null;
            if (!string.IsNullOrEmpty(request.BlockerCategory))
            {
                if (!Enum.TryParse<BlockerCategory>(request.BlockerCategory, out var parsedCategory))
                    throw new DomainException($"Invalid blocker category: {request.BlockerCategory}");
                blockerCategory = parsedCategory;
            }

            checkIn.UpdateEvening(
                top1Completed: request.Top1Completed,
                energyLevelPm: request.EnergyLevelPm,
                stressLevel: request.StressLevel,
                reflection: request.Reflection,
                blockerCategory: blockerCategory,
                blockerNote: request.BlockerNote);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
