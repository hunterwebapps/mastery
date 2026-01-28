using Mastery.Domain.Entities.Experiment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class ExperimentNoteConfiguration : IEntityTypeConfiguration<ExperimentNote>
{
    public void Configure(EntityTypeBuilder<ExperimentNote> builder)
    {
        builder.ToTable("ExperimentNotes");

        builder.HasKey(x => x.Id);

        // Index on ExperimentId for loading experiment's notes
        builder.HasIndex(x => x.ExperimentId);

        builder.Property(x => x.ExperimentId)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
