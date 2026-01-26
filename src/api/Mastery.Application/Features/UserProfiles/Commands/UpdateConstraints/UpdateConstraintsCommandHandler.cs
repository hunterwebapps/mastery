using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateConstraints;

public sealed class UpdateConstraintsCommandHandler : ICommandHandler<UpdateConstraintsCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateConstraintsCommandHandler(
        IUserProfileRepository userProfileRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateConstraintsCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        var constraints = MapConstraints(request.Constraints);
        profile.UpdateConstraints(constraints);

        await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Constraints MapConstraints(ConstraintsDto dto)
    {
        return new Constraints
        {
            MaxPlannedMinutesWeekday = dto.MaxPlannedMinutesWeekday,
            MaxPlannedMinutesWeekend = dto.MaxPlannedMinutesWeekend,
            BlockedTimeWindows = dto.BlockedTimeWindows.Select(b => new BlockedWindow
            {
                Label = b.Label,
                TimeWindow = TimeWindow.Create(b.TimeWindow.Start, b.TimeWindow.End),
                ApplicableDays = b.ApplicableDays
            }).ToList(),
            NoNotificationsWindows = dto.NoNotificationsWindows
                .Select(w => TimeWindow.Create(w.Start, w.End))
                .ToList(),
            HealthNotes = dto.HealthNotes,
            ContentBoundaries = dto.ContentBoundaries
        };
    }
}
