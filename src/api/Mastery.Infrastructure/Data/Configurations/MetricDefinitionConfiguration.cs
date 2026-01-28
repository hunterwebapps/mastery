using System.Text.Json;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class MetricDefinitionConfiguration : IEntityTypeConfiguration<MetricDefinition>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<MetricDefinition> builder)
    {
        builder.ToTable("MetricDefinitions");

        builder.HasKey(x => x.Id);

        // Index on UserId for fast lookups
        builder.HasIndex(x => x.UserId);

        // Unique constraint on UserId + Name
        builder.HasIndex(x => new { x.UserId, x.Name }).IsUnique();

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.DataType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Direction)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.DefaultCadence)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.DefaultAggregation)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        // Unit as JSON
        builder.Property(x => x.Unit)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<MetricUnit>(v, JsonOptions)!);

        // Tags as JSON
        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property<List<string>>("_tags")
            .HasColumnName("Tags")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
