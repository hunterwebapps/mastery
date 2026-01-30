using Mastery.Domain.Entities.Signal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class SignalEntryConfiguration : IEntityTypeConfiguration<SignalEntry>
{
    public void Configure(EntityTypeBuilder<SignalEntry> builder)
    {
        builder.ToTable("SignalEntries");

        // BIGINT IDENTITY primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .UseIdentityColumn();

        // User identification
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        // Event identification
        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EventDataJson)
            .HasMaxLength(4000);

        // Priority and window
        builder.Property(x => x.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.WindowType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ScheduledWindowStart);

        // Target entity (optional)
        builder.Property(x => x.TargetEntityType)
            .HasMaxLength(100);

        builder.Property(x => x.TargetEntityId);

        // Status
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ProcessedAt);

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        // Lease management
        builder.Property(x => x.LeasedUntil);

        builder.Property(x => x.LeaseHolder)
            .HasMaxLength(100);

        // Retry tracking
        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastError)
            .HasMaxLength(500);

        // Processing result
        builder.Property(x => x.ProcessingTier)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.SkipReason)
            .HasMaxLength(500);

        // Deferral tracking for embedding race condition
        builder.Property(x => x.DeferralCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.NextProcessAfter);

        // Index: Urgent signal polling - WHERE Priority='Urgent' AND Status='Pending'
        builder.HasIndex(x => new { x.Priority, x.Status, x.CreatedAt })
            .HasDatabaseName("IX_SignalEntries_Priority_Status_CreatedAt");

        // Index: Window-aligned signal polling - WHERE WindowType AND Status AND ScheduledWindowStart
        builder.HasIndex(x => new { x.WindowType, x.Status, x.ScheduledWindowStart })
            .HasDatabaseName("IX_SignalEntries_WindowType_Status_ScheduledWindowStart")
            .HasFilter("[Status] = 'Pending'");

        // Index: User-scoped signal retrieval
        builder.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt })
            .HasDatabaseName("IX_SignalEntries_UserId_Status_CreatedAt");

        // Index: Expired lease cleanup
        builder.HasIndex(x => new { x.Status, x.LeasedUntil })
            .HasDatabaseName("IX_SignalEntries_Status_LeasedUntil")
            .HasFilter("[Status] = 'Processing'");

        // Index: TTL expiration check
        builder.HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasDatabaseName("IX_SignalEntries_Status_ExpiresAt")
            .HasFilter("[Status] = 'Pending'");

        // Index: Entity deduplication lookup
        builder.HasIndex(x => new { x.UserId, x.EventType, x.TargetEntityType, x.TargetEntityId, x.Status })
            .HasDatabaseName("IX_SignalEntries_Deduplication");

        // Index: Deferral-aware signal acquisition
        builder.HasIndex(x => new { x.Status, x.Priority, x.NextProcessAfter, x.CreatedAt })
            .HasDatabaseName("IX_SignalEntries_Status_Priority_NextProcessAfter")
            .HasFilter("[Status] = 'Pending'");
    }
}
