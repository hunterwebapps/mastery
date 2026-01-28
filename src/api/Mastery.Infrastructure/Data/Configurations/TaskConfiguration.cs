using System.Text.Json;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Domain.Entities.Task.Task>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Domain.Entities.Task.Task> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(x => x.Id);

        // Index on UserId for fast lookups
        builder.HasIndex(x => x.UserId);

        // Composite index for common query: user's tasks by status
        builder.HasIndex(x => new { x.UserId, x.Status });

        // Index for display ordering
        builder.HasIndex(x => new { x.UserId, x.DisplayOrder });

        // Index for project queries
        builder.HasIndex(x => x.ProjectId);

        // Index for goal queries
        builder.HasIndex(x => x.GoalId);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Priority)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(x => x.EstimatedMinutes)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(x => x.EnergyCost)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(x => x.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.RescheduleCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastRescheduleReason)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Due as JSON (nullable value object)
        builder.Property(x => x.Due)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                v => v == null ? null : JsonSerializer.Deserialize<TaskDue>(v, JsonOptions));

        // Scheduling as JSON (nullable value object)
        builder.Property(x => x.Scheduling)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                v => v == null ? null : JsonSerializer.Deserialize<TaskScheduling>(v, JsonOptions));

        // Completion as JSON (nullable value object)
        builder.Property(x => x.Completion)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                v => v == null ? null : JsonSerializer.Deserialize<TaskCompletion>(v, JsonOptions));

        // Value comparers for JSON collections
        var guidListComparer = new ValueComparer<List<Guid>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        var contextTagListComparer = new ValueComparer<List<ContextTag>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        // ContextTags as JSON
        builder.Property<List<ContextTag>>("_contextTags")
            .HasColumnName("ContextTags")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ContextTag>>(v, JsonOptions) ?? new List<ContextTag>())
            .Metadata.SetValueComparer(contextTagListComparer);

        // DependencyTaskIds as JSON
        builder.Property<List<Guid>>("_dependencyTaskIds")
            .HasColumnName("DependencyTaskIds")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .Metadata.SetValueComparer(guidListComparer);

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

        // Relationships
        builder.HasMany(x => x.MetricBindings)
            .WithOne()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        // Ignore domain events and read-only collections
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.ContextTags);
        builder.Ignore(x => x.DependencyTaskIds);
        builder.Ignore(x => x.RoleIds);
        builder.Ignore(x => x.ValueIds);
    }
}
