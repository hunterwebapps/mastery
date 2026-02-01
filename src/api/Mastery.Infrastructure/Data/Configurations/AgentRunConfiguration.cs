using Mastery.Domain.Entities.Recommendation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class AgentRunConfiguration : IEntityTypeConfiguration<AgentRun>
{
    public void Configure(EntityTypeBuilder<AgentRun> builder)
    {
        builder.ToTable("AgentRuns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecommendationTraceId)
            .IsRequired();

        builder.Property(x => x.UserId);

        builder.Property(x => x.Stage)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.InputTokens)
            .IsRequired();

        builder.Property(x => x.OutputTokens)
            .IsRequired();

        builder.Property(x => x.LatencyMs)
            .IsRequired();

        builder.Property(x => x.ErrorType)
            .HasMaxLength(100);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .IsRequired();

        // New LLM tracking properties (all nullable for backward compatibility)
        builder.Property(x => x.CachedInputTokens);

        builder.Property(x => x.ReasoningTokens);

        builder.Property(x => x.SystemFingerprint)
            .HasMaxLength(100);

        builder.Property(x => x.RequestId)
            .HasMaxLength(100);

        builder.Property(x => x.Provider)
            .HasMaxLength(50);

        // Index for querying by trace
        builder.HasIndex(x => x.RecommendationTraceId);

        // Index for analytics (model usage, errors)
        builder.HasIndex(x => new { x.Model, x.StartedAt });
        builder.HasIndex(x => x.ErrorType)
            .HasFilter("[ErrorType] IS NOT NULL");

        // Index for cost analytics by provider
        builder.HasIndex(x => new { x.Provider, x.Model, x.StartedAt })
            .HasDatabaseName("IX_AgentRuns_Provider_Model_StartedAt");

        // Index for per-user cost analytics
        builder.HasIndex(x => new { x.UserId, x.StartedAt })
            .HasDatabaseName("IX_AgentRuns_UserId_StartedAt")
            .HasFilter("[UserId] IS NOT NULL");

        builder.Ignore(x => x.DomainEvents);
    }
}
