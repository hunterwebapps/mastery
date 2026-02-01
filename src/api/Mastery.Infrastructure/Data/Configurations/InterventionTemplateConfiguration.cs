using Mastery.Domain.Entities.Intervention;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class InterventionTemplateConfiguration : IEntityTypeConfiguration<InterventionTemplate>
{
    public void Configure(EntityTypeBuilder<InterventionTemplate> builder)
    {
        builder.ToTable("InterventionTemplates");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsActive);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.TitleTemplate)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.RationaleTemplate)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ParametersSchema)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.DefaultRecommendationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.DefaultActionKind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.DefaultTargetKind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Ignore(x => x.DomainEvents);
    }
}
