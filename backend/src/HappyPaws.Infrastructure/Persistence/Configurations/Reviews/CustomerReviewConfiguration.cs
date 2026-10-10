using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Reviews;

/// <summary>
/// Customer reviews, one row per reviewed visit (<c>docs/contract-extensions.md § 36</c>).
/// </summary>
internal sealed class CustomerReviewConfiguration : EntityConfiguration<CustomerReview>
{
    /// <summary>One review per visit. Named so <c>PostgresErrorTranslator</c> can answer two that race in.</summary>
    public const string VisitIndex = "ix_customer_reviews_visit";

    protected override void ConfigureEntity(EntityTypeBuilder<CustomerReview> builder)
    {
        builder.ToTable("customer_reviews", table =>
        {
            table.HasCheckConstraint("ck_customer_reviews_rating", "rating BETWEEN 1 AND 5");

            // A rejected review always says why, and only from the list; a published one says when.
            table.HasCheckConstraint(
                "ck_customer_reviews_rejected_has_reason",
                "(status = 'Rejected') = (reject_reason IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_customer_reviews_published_has_time",
                "status <> 'Published' OR published_at IS NOT NULL");
        });

        builder.Property(review => review.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(review => review.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(review => review.VisitId).HasMaxLength(40).IsRequired();
        builder.Property(review => review.PetId).HasMaxLength(40);
        builder.Property(review => review.PetSpecies).HasMaxLength(60);
        builder.Property(review => review.Text).HasMaxLength(ReviewText.MaxLength);
        builder.Property(review => review.PublicName).HasMaxLength(ReviewNames.MaxLength);
        builder.Property(review => review.NameChoice).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(review => review.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(review => review.InvitationId).HasMaxLength(40);
        builder.Property(review => review.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(review => review.ModeratedByStaffId).HasMaxLength(40);
        builder.Property(review => review.RejectReason).HasConversion<string>().HasMaxLength(30);
        builder.Property(review => review.ReplyText).HasMaxLength(ReviewText.ReplyMaxLength);
        builder.Property(review => review.RepliedByStaffId).HasMaxLength(40);

        builder.Ignore(review => review.IsPublished);

        builder.HasOne<Customer>().WithMany().HasForeignKey(review => review.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(review => review.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(review => new { review.CustomerId, review.BranchId, review.VisitOn })
            .IsUnique()
            .HasDatabaseName(VisitIndex);

        // The moderation tabs and the public list: one status, newest written first.
        builder.HasIndex(review => new { review.Status, review.CreatedAt, review.Id });
    }
}
