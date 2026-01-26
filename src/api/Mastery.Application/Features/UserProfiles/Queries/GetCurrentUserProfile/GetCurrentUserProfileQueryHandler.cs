using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.UserProfiles.Queries.GetCurrentUserProfile;

public sealed class GetCurrentUserProfileQueryHandler : IQueryHandler<GetCurrentUserProfileQuery, UserProfileDto?>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserProfileQueryHandler(
        IUserProfileRepository userProfileRepository,
        ICurrentUserService currentUserService)
    {
        _userProfileRepository = userProfileRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileDto?> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return null;

        var profile = await _userProfileRepository.GetByUserIdWithSeasonAsync(userId, cancellationToken);
        if (profile == null)
            return null;

        return MapToDto(profile);
    }

    private static UserProfileDto MapToDto(UserProfile profile)
    {
        return new UserProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Timezone = profile.Timezone.IanaId,
            Locale = profile.Locale.Code,
            OnboardingVersion = profile.OnboardingVersion,
            Values = profile.Values.Select(v => new UserValueDto
            {
                Id = v.Id,
                Key = v.Key,
                Label = v.Label,
                Rank = v.Rank,
                Weight = v.Weight,
                Notes = v.Notes,
                Source = v.Source
            }).ToList(),
            Roles = profile.Roles.Select(r => new UserRoleDto
            {
                Id = r.Id,
                Key = r.Key,
                Label = r.Label,
                Rank = r.Rank,
                SeasonPriority = r.SeasonPriority,
                MinWeeklyMinutes = r.MinWeeklyMinutes,
                TargetWeeklyMinutes = r.TargetWeeklyMinutes,
                Tags = r.Tags,
                Status = r.Status.ToString()
            }).ToList(),
            CurrentSeason = profile.CurrentSeason != null ? MapSeasonToDto(profile.CurrentSeason) : null,
            Preferences = MapPreferencesToDto(profile.Preferences),
            Constraints = MapConstraintsToDto(profile.Constraints),
            CreatedAt = profile.CreatedAt,
            ModifiedAt = profile.ModifiedAt
        };
    }

    private static SeasonDto MapSeasonToDto(Domain.Entities.Season season)
    {
        return new SeasonDto
        {
            Id = season.Id,
            UserId = season.UserId,
            Label = season.Label,
            Type = season.Type.ToString(),
            StartDate = season.StartDate,
            ExpectedEndDate = season.ExpectedEndDate,
            ActualEndDate = season.ActualEndDate,
            FocusRoleIds = season.FocusRoleIds.ToList(),
            FocusGoalIds = season.FocusGoalIds.ToList(),
            SuccessStatement = season.SuccessStatement,
            NonNegotiables = season.NonNegotiables.ToList(),
            Intensity = season.Intensity,
            Outcome = season.Outcome,
            IsEnded = season.IsEnded,
            CreatedAt = season.CreatedAt,
            ModifiedAt = season.ModifiedAt
        };
    }

    private static PreferencesDto MapPreferencesToDto(Preferences preferences)
    {
        return new PreferencesDto
        {
            CoachingStyle = preferences.CoachingStyle.ToString(),
            ExplanationVerbosity = preferences.ExplanationVerbosity.ToString(),
            NudgeLevel = preferences.NudgeLevel.ToString(),
            NotificationChannels = preferences.NotificationChannels.Select(c => c.ToString()).ToList(),
            MorningCheckInTime = preferences.CheckInSchedule.MorningTime,
            EveningCheckInTime = preferences.CheckInSchedule.EveningTime,
            PlanningDefaults = new PlanningDefaultsDto
            {
                DefaultTaskDurationMinutes = preferences.PlanningDefaults.DefaultTaskDurationMinutes,
                AutoScheduleHabits = preferences.PlanningDefaults.AutoScheduleHabits,
                BufferBetweenTasksMinutes = preferences.PlanningDefaults.BufferBetweenTasksMinutes
            },
            Privacy = new PrivacySettingsDto
            {
                ShareProgressWithCoach = preferences.Privacy.ShareProgressWithCoach,
                AllowAnonymousAnalytics = preferences.Privacy.AllowAnonymousAnalytics
            }
        };
    }

    private static ConstraintsDto MapConstraintsToDto(Constraints constraints)
    {
        return new ConstraintsDto
        {
            MaxPlannedMinutesWeekday = constraints.MaxPlannedMinutesWeekday,
            MaxPlannedMinutesWeekend = constraints.MaxPlannedMinutesWeekend,
            BlockedTimeWindows = constraints.BlockedTimeWindows.Select(b => new BlockedWindowDto
            {
                Label = b.Label,
                TimeWindow = new TimeWindowDto
                {
                    Start = b.TimeWindow.Start,
                    End = b.TimeWindow.End
                },
                ApplicableDays = b.ApplicableDays
            }).ToList(),
            NoNotificationsWindows = constraints.NoNotificationsWindows.Select(w => new TimeWindowDto
            {
                Start = w.Start,
                End = w.End
            }).ToList(),
            HealthNotes = constraints.HealthNotes,
            ContentBoundaries = constraints.ContentBoundaries
        };
    }
}
