using System.Text.Json;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.ToTable("Habits");

        builder.HasKey(x => x.Id);

        // Index on UserId for fast lookups
        builder.HasIndex(x => x.UserId);

        // Composite index for common query: user's habits by status
        builder.HasIndex(x => new { x.UserId, x.Status });

        // Index for display ordering
        builder.HasIndex(x => new { x.UserId, x.DisplayOrder });

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Why)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.DefaultMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(HabitMode.Full);

        builder.Property(x => x.CurrentStreak)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.AdherenceRate7Day)
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        // Schedule as JSON (complex value object)
        builder.Property(x => x.Schedule)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<HabitSchedule>(v, JsonOptions)!);

        // Policy as JSON (complex value object)
        builder.Property(x => x.Policy)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<HabitPolicy>(v, JsonOptions)!);

        // Value comparers for JSON collections
        var guidListComparer = new ValueComparer<List<Guid>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        // RoleIds as JSON
        builder.Property<List<Guid>>("_roleIds")
            .HasColumnName("RoleIds")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .Metadata.SetValueComparer(guidListComparer);

        // ValueIds as JSON
        builder.Property<List<Guid>>("_valueIds")
            .HasColumnName("ValueIds")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .Metadata.SetValueComparer(guidListComparer);

        // GoalIds as JSON
        builder.Property<List<Guid>>("_goalIds")
            .HasColumnName("GoalIds")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .Metadata.SetValueComparer(guidListComparer);

        // Relationships
        builder.HasMany(x => x.MetricBindings)
            .WithOne()
            .HasForeignKey(x => x.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Variants)
            .WithOne()
            .HasForeignKey(x => x.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Occurrences)
            .WithOne()
            .HasForeignKey(x => x.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        // Ignore domain events and read-only collections
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.RoleIds);
        builder.Ignore(x => x.ValueIds);
        builder.Ignore(x => x.GoalIds);
    }
}
