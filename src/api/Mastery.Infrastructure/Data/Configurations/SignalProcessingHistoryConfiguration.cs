using Mastery.Domain.Entities.Signal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class SignalProcessingHistoryConfiguration : IEntityTypeConfiguration<SignalProcessingHistory>
{
    public void Configure(EntityTypeBuilder<SignalProcessingHistory> builder)
    {
        builder.ToTable("SignalProcessingHistory");

        builder.HasKey(x => x.Id);

        // User identification
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        // Window type
        builder.Property(x => x.WindowType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Timestamps
        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.DurationMs);

        // Batch ID for idempotency
        builder.Property(x => x.BatchId)
            .IsRequired();

        // Signal counts
        builder.Property(x => x.SignalsReceived)
            .IsRequired();

        builder.Property(x => x.SignalsProcessed)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.SignalsSkipped)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.SignalIdsJson)
            .HasMaxLength(4000);

        // Assessment results
        builder.Property(x => x.FinalTier)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Tier0RulesTriggered)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Tier1CombinedScore)
            .HasPrecision(5, 4);

        builder.Property(x => x.Tier2Executed)
            .IsRequired()
            .HasDefaultValue(false);

        // Recommendation outcome
        builder.Property(x => x.RecommendationsGenerated)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.RecommendationIdsJson)
            .HasMaxLength(4000);

        // Error tracking
        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        // State delta
        builder.Property(x => x.StateDeltaSummaryJson)
            .HasMaxLength(8000);

        // Index: User processing history lookup
        builder.HasIndex(x => new { x.UserId, x.StartedAt })
            .HasDatabaseName("IX_SignalProcessingHistory_UserId_StartedAt")
            .IsDescending(false, true);

        // Index: Recent processing cycles
        builder.HasIndex(x => x.StartedAt)
            .HasDatabaseName("IX_SignalProcessingHistory_StartedAt")
            .IsDescending(true);

        // Index: Window type analysis
        builder.HasIndex(x => new { x.WindowType, x.StartedAt })
            .HasDatabaseName("IX_SignalProcessingHistory_WindowType_StartedAt");

        // Index: Batch ID for idempotency checks
        builder.HasIndex(x => x.BatchId)
            .IsUnique()
            .HasDatabaseName("IX_SignalProcessingHistory_BatchId");

        // Ignore domain events (from BaseEntity)
        builder.Ignore(x => x.DomainEvents);
    }
}
