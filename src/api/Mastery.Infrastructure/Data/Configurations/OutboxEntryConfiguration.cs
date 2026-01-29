using Mastery.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class OutboxEntryConfiguration : IEntityTypeConfiguration<OutboxEntry>
{
    public void Configure(EntityTypeBuilder<OutboxEntry> builder)
    {
        builder.ToTable("OutboxEntries");

        // BIGINT IDENTITY primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .UseIdentityColumn();

        // Entity identification
        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .IsRequired();

        builder.Property(x => x.Operation)
            .IsRequired()
            .HasMaxLength(20);

        // User scope (nullable)
        builder.Property(x => x.UserId)
            .HasMaxLength(256);

        // Timestamps
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ProcessedAt);

        // Lease management
        builder.Property(x => x.LeasedUntil);

        builder.Property(x => x.LeaseHolder)
            .HasMaxLength(100);

        // Retry tracking
        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        // Status as string for readability
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Index: Primary polling query - WHERE Status='Pending' ORDER BY CreatedAt
        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_OutboxEntries_Status_CreatedAt");

        // Index: Expired lease cleanup - filtered to Processing entries only
        builder.HasIndex(x => new { x.Status, x.LeasedUntil })
            .HasDatabaseName("IX_OutboxEntries_Status_LeasedUntil")
            .HasFilter("[Status] = 'Processing'");

        // Index: Entity lookup and deduplication
        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_OutboxEntries_EntityType_EntityId");

        // Index: User-scoped processing - filtered to non-null UserId
        builder.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt })
            .HasDatabaseName("IX_OutboxEntries_UserId_Status_CreatedAt")
            .HasFilter("[UserId] IS NOT NULL");

        // Index: Archival query - filtered to Processed entries
        builder.HasIndex(x => new { x.Status, x.ProcessedAt })
            .HasDatabaseName("IX_OutboxEntries_Status_ProcessedAt")
            .HasFilter("[Status] = 'Processed'");
    }
}
