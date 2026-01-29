using System.Text.Json;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.ValueObjects;
using Mastery.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(x => x.Id);

        // UserId index for fast lookups
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450); // Match ASP.NET Identity's Id column length

        // FK relationship to ApplicationUser (one-to-one)
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(x => x.UserId)
            .HasPrincipalKey<ApplicationUser>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.OnboardingVersion)
            .IsRequired()
            .HasDefaultValue(1);

        // Timezone value object
        builder.OwnsOne(x => x.Timezone, timezone =>
        {
            timezone.Property(t => t.IanaId)
                .HasColumnName("Timezone")
                .HasMaxLength(50)
                .IsRequired();
        });

        // Locale value object
        builder.OwnsOne(x => x.Locale, locale =>
        {
            locale.Property(l => l.Code)
                .HasColumnName("Locale")
                .HasMaxLength(10)
                .IsRequired();
        });

        // Values as JSON column - use backing field directly
        var valuesComparer = new ValueComparer<List<UserValue>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property<List<UserValue>>("_values")
            .HasColumnName("Values")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<UserValue>>(v, JsonOptions) ?? new List<UserValue>())
            .Metadata.SetValueComparer(valuesComparer);

        builder.Ignore(x => x.Values);

        // Roles as JSON column - use backing field directly
        var rolesComparer = new ValueComparer<List<UserRole>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property<List<UserRole>>("_roles")
            .HasColumnName("Roles")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<UserRole>>(v, JsonOptions) ?? new List<UserRole>())
            .Metadata.SetValueComparer(rolesComparer);

        builder.Ignore(x => x.Roles);

        // FK to Season
        builder.Property(x => x.CurrentSeasonId);

        builder.HasOne(x => x.CurrentSeason)
            .WithMany()
            .HasForeignKey(x => x.CurrentSeasonId)
            .OnDelete(DeleteBehavior.SetNull);

        // Value comparers for owned entity collections
        var notificationChannelsComparer = new ValueComparer<List<NotificationChannel>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        var blockedWindowsComparer = new ValueComparer<List<BlockedWindow>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        var timeWindowsComparer = new ValueComparer<List<TimeWindow>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        // Preferences as owned entity
        builder.OwnsOne(x => x.Preferences, prefs =>
        {
            prefs.Property(p => p.CoachingStyle)
                .HasColumnName("CoachingStyle")
                .HasConversion<string>()
                .HasMaxLength(20);

            prefs.Property(p => p.ExplanationVerbosity)
                .HasColumnName("ExplanationVerbosity")
                .HasConversion<string>()
                .HasMaxLength(20);

            prefs.Property(p => p.NudgeLevel)
                .HasColumnName("NudgeLevel")
                .HasConversion<string>()
                .HasMaxLength(20);

            prefs.Property(p => p.NotificationChannels)
                .HasColumnName("NotificationChannels")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<NotificationChannel>>(v, JsonOptions) ?? new List<NotificationChannel>())
                .Metadata.SetValueComparer(notificationChannelsComparer);

            // CheckInSchedule as nested owned type
            prefs.OwnsOne(p => p.CheckInSchedule, schedule =>
            {
                schedule.Property(s => s.MorningTime)
                    .HasColumnName("CheckInMorningTime");

                schedule.Property(s => s.EveningTime)
                    .HasColumnName("CheckInEveningTime");
            });

            // PlanningDefaults as nested owned type
            prefs.OwnsOne(p => p.PlanningDefaults, pd =>
            {
                pd.Property(x => x.DefaultTaskDurationMinutes)
                    .HasColumnName("DefaultTaskDurationMinutes")
                    .HasDefaultValue(30);

                pd.Property(x => x.AutoScheduleHabits)
                    .HasColumnName("AutoScheduleHabits")
                    .HasDefaultValue(true);

                pd.Property(x => x.BufferBetweenTasksMinutes)
                    .HasColumnName("BufferBetweenTasksMinutes")
                    .HasDefaultValue(5);
            });

            // Privacy as nested owned type
            prefs.OwnsOne(p => p.Privacy, privacy =>
            {
                privacy.Property(x => x.ShareProgressWithCoach)
                    .HasColumnName("ShareProgressWithCoach")
                    .HasDefaultValue(false);

                privacy.Property(x => x.AllowAnonymousAnalytics)
                    .HasColumnName("AllowAnonymousAnalytics")
                    .HasDefaultValue(true);
            });

            // ProcessingWindows as nested owned type
            prefs.OwnsOne(p => p.ProcessingWindows, pw =>
            {
                pw.Property(x => x.MorningWindowStart)
                    .HasColumnName("MorningWindowStart");

                pw.Property(x => x.MorningWindowEnd)
                    .HasColumnName("MorningWindowEnd");

                pw.Property(x => x.EveningWindowStart)
                    .HasColumnName("EveningWindowStart");

                pw.Property(x => x.EveningWindowEnd)
                    .HasColumnName("EveningWindowEnd");

                pw.Property(x => x.WeeklyReviewDay)
                    .HasColumnName("WeeklyReviewDay")
                    .HasConversion<string>()
                    .HasMaxLength(10);

                pw.Property(x => x.WeeklyReviewStart)
                    .HasColumnName("WeeklyReviewStart");

                pw.Property(x => x.WeeklyReviewEnd)
                    .HasColumnName("WeeklyReviewEnd");
            });
        });

        // Constraints as owned entity
        builder.OwnsOne(x => x.Constraints, constraints =>
        {
            constraints.Property(c => c.MaxPlannedMinutesWeekday)
                .HasColumnName("MaxPlannedMinutesWeekday")
                .HasDefaultValue(480);

            constraints.Property(c => c.MaxPlannedMinutesWeekend)
                .HasColumnName("MaxPlannedMinutesWeekend")
                .HasDefaultValue(240);

            constraints.Property(c => c.BlockedTimeWindows)
                .HasColumnName("BlockedTimeWindows")
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<BlockedWindow>>(v, JsonOptions) ?? new List<BlockedWindow>())
                .Metadata.SetValueComparer(blockedWindowsComparer);

            constraints.Property(c => c.NoNotificationsWindows)
                .HasColumnName("NoNotificationsWindows")
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<TimeWindow>>(v, JsonOptions) ?? new List<TimeWindow>())
                .Metadata.SetValueComparer(timeWindowsComparer);

            constraints.Property(c => c.HealthNotes)
                .HasColumnName("HealthNotes")
                .HasMaxLength(1000);

            constraints.Property(c => c.ContentBoundaries)
                .HasColumnName("ContentBoundaries")
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);
        });

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        // Ignore domain events (handled elsewhere)
        builder.Ignore(x => x.DomainEvents);
    }
}
