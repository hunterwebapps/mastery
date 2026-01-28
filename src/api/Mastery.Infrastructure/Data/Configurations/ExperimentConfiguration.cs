using System.Text.Json;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class ExperimentConfiguration : IEntityTypeConfiguration<Experiment>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Experiment> builder)
    {
        builder.ToTable("Experiments");

        builder.HasKey(x => x.Id);

        // Index on UserId for fast lookups
        builder.HasIndex(x => x.UserId);

        // Composite index for common query: user's experiments by status
        builder.HasIndex(x => new { x.UserId, x.Status });

        // Unique filtered index: at-most-one-active invariant
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[Status] = 'Active'")
            .HasDatabaseName("IX_Experiments_UserId_ActiveUnique");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedFrom)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        // Hypothesis as JSON (complex value object)
        builder.Property(x => x.Hypothesis)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Hypothesis>(v, JsonOptions)!);

        // MeasurementPlan as JSON (complex value object)
        builder.Property(x => x.MeasurementPlan)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<MeasurementPlan>(v, JsonOptions)!);

        builder.Property(x => x.StartDate);

        builder.Property(x => x.EndDatePlanned);

        builder.Property(x => x.EndDateActual);

        // Value comparers for JSON collections
        var guidListComparer = new ValueComparer<List<Guid>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        // LinkedGoalIds as JSON
        builder.Property<List<Guid>>("_linkedGoalIds")
            .HasColumnName("LinkedGoalIds")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .Metadata.SetValueComparer(guidListComparer);

        // Relationships
        builder.HasMany(x => x.Notes)
            .WithOne()
            .HasForeignKey(x => x.ExperimentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Result)
            .WithOne()
            .HasForeignKey<ExperimentResult>(x => x.ExperimentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        // Ignore domain events and read-only collections
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.LinkedGoalIds);
    }
}
