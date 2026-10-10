using HappyPaws.Domain.PrivacyPolicies;
using HappyPaws.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.PrivacyPolicies;

/// <summary>
/// The clinic's privacy notice, one row per version (<c>docs/contract-extensions.md § 35</c>). The
/// sections are one <c>jsonb</c> value: read and replaced as a whole, never queried into.
/// </summary>
internal sealed class PrivacyPolicyVersionConfiguration : EntityConfiguration<PrivacyPolicyVersion>
{
    /// <summary>Named so <c>PostgresErrorTranslator</c> can answer two owners racing for one label.</summary>
    public const string LabelIndex = "ix_privacy_policy_versions_label";

    protected override void ConfigureEntity(EntityTypeBuilder<PrivacyPolicyVersion> builder)
    {
        builder.ToTable("privacy_policy_versions", table =>
        {
            // Published means all three are set, a draft has none of them: a consent's label always
            // names a version with a date and a moment it took effect.
            table.HasCheckConstraint(
                "ck_privacy_policy_versions_published_complete",
                "(published_at IS NULL AND label IS NULL AND effective_date IS NULL) " +
                "OR (published_at IS NOT NULL AND label IS NOT NULL AND effective_date IS NOT NULL)");
        });

        builder.Property(version => version.Sections)
            .HasField("_sections")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasJsonListConversion()
            .IsRequired();

        builder.Property(version => version.CreatedFrom).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(version => version.TemplateVersion).HasMaxLength(40);
        builder.Property(version => version.BasedOnVersionId).HasMaxLength(40);
        builder.Property(version => version.CreatedByStaffId).HasMaxLength(40);
        builder.Property(version => version.Label).HasMaxLength(PrivacyPolicyRules.LabelMaxLength);
        builder.Property(version => version.PublishedByStaffId).HasMaxLength(40);

        builder.Ignore(version => version.IsPublished);
        builder.Ignore(version => version.IsFromTemplate);

        // A label is what a consent points at, so two published versions cannot share one. Drafts
        // have none, and PostgreSQL lets any number of rows have none.
        builder.HasIndex(version => version.Label).IsUnique().HasDatabaseName(LabelIndex);

        // "The current notice" is the newest published one, read on every consent.
        builder.HasIndex(version => version.PublishedAt);
    }
}
