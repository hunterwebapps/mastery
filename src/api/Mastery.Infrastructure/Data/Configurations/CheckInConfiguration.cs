using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
{
    public void Configure(EntityTypeBuilder<CheckIn> builder)
    {
        builder.ToTable("CheckIns");

        builder.HasKey(x => x.Id);

        // Index on UserId for fast lookups
        builder.HasIndex(x => x.UserId);

        // Unique composite index: one morning + one evening per user per date
        builder.HasIndex(x => new { x.UserId, x.CheckInDate, x.Type })
            .IsUnique();

        // Composite index for common query: user's check-ins by status
        builder.HasIndex(x => new { x.UserId, x.Status });

        // Composite index for date range queries
        builder.HasIndex(x => new { x.UserId, x.CheckInDate });

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.CheckInDate)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CompletedAt);

        // Morning fields (all nullable since evening check-ins won't have them)
        builder.Property(x => x.EnergyLevel);

        builder.Property(x => x.SelectedMode)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Top1Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Top1EntityId);

        builder.Property(x => x.Top1FreeText)
            .HasMaxLength(200);

        builder.Property(x => x.Intention)
            .HasMaxLength(500);

        // Evening fields (all nullable since morning check-ins won't have them)
        builder.Property(x => x.EnergyLevelPm);

        builder.Property(x => x.StressLevel);

        builder.Property(x => x.Reflection)
            .HasMaxLength(1000);

        builder.Property(x => x.BlockerCategory)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.BlockerNote)
            .HasMaxLength(500);

        builder.Property(x => x.Top1Completed);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
