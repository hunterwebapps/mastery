using Mastery.Domain.Entities.Recommendation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class RecommendationTraceConfiguration : IEntityTypeConfiguration<RecommendationTrace>
{
    public void Configure(EntityTypeBuilder<RecommendationTrace> builder)
    {
        builder.ToTable("RecommendationTraces");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecommendationId)
            .IsRequired();

        builder.Property(x => x.StateSnapshotJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.SignalsSummaryJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.CandidateListJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.PromptVersion)
            .HasMaxLength(100);

        builder.Property(x => x.ModelVersion)
            .HasMaxLength(100);

        builder.Property(x => x.RawLlmResponse)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.SelectionMethod)
            .IsRequired()
            .HasMaxLength(50);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        builder.Ignore(x => x.DomainEvents);
    }
}
