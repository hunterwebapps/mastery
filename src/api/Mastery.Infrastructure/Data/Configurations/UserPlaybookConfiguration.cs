using Mastery.Domain.Entities.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class UserPlaybookConfiguration : IEntityTypeConfiguration<UserPlaybook>
{
    public void Configure(EntityTypeBuilder<UserPlaybook> builder)
    {
        builder.ToTable("UserPlaybooks");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.TotalOutcomes)
            .IsRequired();

        builder.Property(x => x.LastUpdatedAt)
            .IsRequired();

        // One-to-many with PlaybookEntry
        builder.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey("UserPlaybookId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}

public class PlaybookEntryConfiguration : IEntityTypeConfiguration<PlaybookEntry>
{
    public void Configure(EntityTypeBuilder<PlaybookEntry> builder)
    {
        builder.ToTable("PlaybookEntries");

        builder.HasKey(x => x.Id);

        builder.HasIndex("UserPlaybookId", "RecommendationType", "ContextKey")
            .IsUnique();

        builder.Property(x => x.RecommendationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ContextKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SuccessWeight)
            .IsRequired()
            .HasPrecision(5, 4);

        builder.Property(x => x.ObservationCount)
            .IsRequired();

        builder.Property(x => x.AcceptanceRate)
            .HasPrecision(5, 4);

        builder.Property(x => x.CompletionRate)
            .HasPrecision(5, 4);

        builder.Property(x => x.AcceptanceCount)
            .IsRequired();

        builder.Property(x => x.CompletionCount)
            .IsRequired();

        builder.Property(x => x.LastUpdatedAt)
            .IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
