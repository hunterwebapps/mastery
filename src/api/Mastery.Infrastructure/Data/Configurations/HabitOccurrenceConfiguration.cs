using Mastery.Domain.Entities.Habit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class HabitOccurrenceConfiguration : IEntityTypeConfiguration<HabitOccurrence>
{
    public void Configure(EntityTypeBuilder<HabitOccurrence> builder)
    {
        builder.ToTable("HabitOccurrences");

        builder.HasKey(x => x.Id);

        // Critical index for Today query and uniqueness
        builder.HasIndex(x => new { x.HabitId, x.ScheduledOn }).IsUnique();

        // Index for status-based queries
        builder.HasIndex(x => new { x.HabitId, x.Status });

        // Index for date-range queries
        builder.HasIndex(x => x.ScheduledOn);

        builder.Property(x => x.HabitId)
            .IsRequired();

        builder.Property(x => x.ScheduledOn)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.CompletedOn);

        builder.Property(x => x.ModeUsed)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.EnteredValue)
            .HasPrecision(18, 4);

        builder.Property(x => x.MissReason)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.RescheduledTo);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
