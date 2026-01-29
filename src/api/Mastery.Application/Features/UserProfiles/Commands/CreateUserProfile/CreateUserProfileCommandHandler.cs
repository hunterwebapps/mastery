using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.UserProfiles.Commands.CreateUserProfile;

public sealed class CreateUserProfileCommandHandler : ICommandHandler<CreateUserProfileCommand, Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserProfileCommandHandler(
        IUserProfileRepository userProfileRepository,
        ISeasonRepository seasonRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
        _seasonRepository = seasonRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        // Use explicit UserId if provided (e.g., during registration), otherwise use current user
        var userId = request.UserId ?? _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Check if profile already exists
        if (await _userProfileRepository.ExistsByUserIdAsync(userId, cancellationToken))
            throw new DomainException("User profile already exists.");

        // Create the profile
        var profile = UserProfile.Create(userId, request.Timezone, request.Locale);

        // Set values
        var values = request.Values.Select(v => new UserValue
        {
            Id = v.Id == Guid.Empty ? Guid.NewGuid() : v.Id,
            Key = v.Key,
            Label = v.Label,
            Rank = v.Rank,
            Weight = v.Weight,
            Notes = v.Notes,
            Source = v.Source ?? "onboarding"
        });
        profile.UpdateValues(values);

        // Set roles
        var roles = request.Roles.Select(r => new UserRole
        {
            Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id,
            Key = r.Key,
            Label = r.Label,
            Rank = r.Rank,
            SeasonPriority = r.SeasonPriority,
            MinWeeklyMinutes = r.MinWeeklyMinutes,
            TargetWeeklyMinutes = r.TargetWeeklyMinutes,
            Tags = r.Tags,
            Status = Enum.TryParse<RoleStatus>(r.Status, out var status) ? status : RoleStatus.Active
        });
        profile.UpdateRoles(roles);

        // Set preferences if provided
        if (request.Preferences != null)
        {
            var preferences = MapPreferences(request.Preferences);
            profile.UpdatePreferences(preferences);
        }

        // Set constraints if provided
        if (request.Constraints != null)
        {
            var constraints = MapConstraints(request.Constraints);
            profile.UpdateConstraints(constraints);
        }

        await _userProfileRepository.AddAsync(profile, cancellationToken);

        // Create initial season if provided
        if (request.InitialSeason != null)
        {
            var seasonType = Enum.TryParse<SeasonType>(request.InitialSeason.Type, out var type)
                ? type
                : SeasonType.Build;

            var season = Season.Create(
                userId,
                request.InitialSeason.Label,
                seasonType,
                request.InitialSeason.StartDate,
                request.InitialSeason.ExpectedEndDate,
                request.InitialSeason.FocusRoleIds,
                request.InitialSeason.FocusGoalIds,
                request.InitialSeason.SuccessStatement,
                request.InitialSeason.NonNegotiables,
                request.InitialSeason.Intensity);

            await _seasonRepository.AddAsync(season, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.Id;
    }

    private static Preferences MapPreferences(Models.PreferencesDto dto)
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

    private static Constraints MapConstraints(Models.ConstraintsDto dto)
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
