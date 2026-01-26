using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdatePreferences;

public sealed class UpdatePreferencesCommandHandler : ICommandHandler<UpdatePreferencesCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePreferencesCommandHandler(
        IUserProfileRepository userProfileRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        var preferences = MapPreferences(request.Preferences);
        profile.UpdatePreferences(preferences);

        await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Preferences MapPreferences(PreferencesDto dto)
    {
        return new Preferences
        {
            CoachingStyle = Enum.TryParse<CoachingStyle>(dto.CoachingStyle, out var style)
                ? style : CoachingStyle.Encouraging,
            ExplanationVerbosity = Enum.TryParse<VerbosityLevel>(dto.ExplanationVerbosity, out var verbosity)
                ? verbosity : VerbosityLevel.Medium,
            NudgeLevel = Enum.TryParse<NudgeLevel>(dto.NudgeLevel, out var nudge)
                ? nudge : NudgeLevel.Medium,
            NotificationChannels = dto.NotificationChannels
                .Select(c => Enum.TryParse<NotificationChannel>(c, out var channel) ? channel : NotificationChannel.Push)
                .ToList(),
            CheckInSchedule = CheckInSchedule.Create(dto.MorningCheckInTime, dto.EveningCheckInTime),
            PlanningDefaults = new PlanningDefaults
            {
                DefaultTaskDurationMinutes = dto.PlanningDefaults.DefaultTaskDurationMinutes,
                AutoScheduleHabits = dto.PlanningDefaults.AutoScheduleHabits,
                BufferBetweenTasksMinutes = dto.PlanningDefaults.BufferBetweenTasksMinutes
            },
            Privacy = new PrivacySettings
            {
                ShareProgressWithCoach = dto.Privacy.ShareProgressWithCoach,
                AllowAnonymousAnalytics = dto.Privacy.AllowAnonymousAnalytics
            }
        };
    }
}
