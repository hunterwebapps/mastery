using System.Text.Json;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");

        builder.HasKey(x => x.Id);

        // Index on UserId for fast lookups
        builder.HasIndex(x => x.UserId);

        // Composite index for common query: user's goals by status
        builder.HasIndex(x => new { x.UserId, x.Status });

        // Index for season-related queries
        builder.HasIndex(x => x.SeasonId)
            .HasFilter("[SeasonId] IS NOT NULL");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Why)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Priority)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(x => x.Deadline);

        builder.Property(x => x.SeasonId);

        builder.Property(x => x.CompletionNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.CompletedAt);

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

        // DependencyIds as JSON
        builder.Property<List<Guid>>("_dependencyIds")
            .HasColumnName("DependencyIds")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .Metadata.SetValueComparer(guidListComparer);

        // Foreign key to Season (optional)
        builder.HasOne<Season>()
            .WithMany()
            .HasForeignKey(x => x.SeasonId)
            .OnDelete(DeleteBehavior.SetNull);

        // One-to-many relationship with GoalMetrics
        builder.HasMany(x => x.Metrics)
            .WithOne()
            .HasForeignKey(x => x.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        // Ignore domain events and read-only collections
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.RoleIds);
        builder.Ignore(x => x.ValueIds);
        builder.Ignore(x => x.DependencyIds);
    }
}
