using Mastery.Domain.Entities.Habit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class HabitVariantConfiguration : IEntityTypeConfiguration<HabitVariant>
{
    public void Configure(EntityTypeBuilder<HabitVariant> builder)
    {
        builder.ToTable("HabitVariants");

        builder.HasKey(x => x.Id);

        // Index on HabitId for loading habit's variants
        builder.HasIndex(x => x.HabitId);

        // Unique constraint: same mode can't exist twice for same habit
        builder.HasIndex(x => new { x.HabitId, x.Mode }).IsUnique();

        builder.Property(x => x.HabitId)
            .IsRequired();

        builder.Property(x => x.Mode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DefaultValue)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.EstimatedMinutes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.EnergyCost)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(x => x.CountsAsCompletion)
            .IsRequired()
            .HasDefaultValue(true);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
