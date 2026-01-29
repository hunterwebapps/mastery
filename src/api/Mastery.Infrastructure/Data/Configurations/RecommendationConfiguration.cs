using System.Text.Json;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendations");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.UserId, x.Context, x.Status });

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Context)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.Kind)
                .HasColumnName("TargetKind")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            target.Property(t => t.EntityId)
                .HasColumnName("TargetEntityId");

            target.Property(t => t.EntityTitle)
                .HasColumnName("TargetEntityTitle")
                .HasMaxLength(300);
        });

        builder.Property(x => x.ActionKind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Rationale)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.ActionPayload)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ActionSummary)
            .HasMaxLength(500);

        builder.Property(x => x.Score)
            .HasPrecision(10, 4);

        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.RespondedAt);

        builder.Property(x => x.DismissReason)
            .HasMaxLength(500);

        // SignalIds as JSON
        var guidListComparer = new ValueComparer<List<Guid>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property<List<Guid>>("_signalIds")
            .HasColumnName("SignalIds")
            .HasColumnType("nvarchar(max)")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>())
            .Metadata.SetValueComparer(guidListComparer);

        // One-to-one with RecommendationTrace
        builder.HasOne(x => x.Trace)
            .WithOne()
            .HasForeignKey<RecommendationTrace>(t => t.RecommendationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.SignalIds);
    }
}
