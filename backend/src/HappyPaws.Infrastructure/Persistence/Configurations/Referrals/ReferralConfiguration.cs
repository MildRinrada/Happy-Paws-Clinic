using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Referrals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Referrals;

/// <summary>
/// One code per customer (<c>docs/contract-extensions.md § 39</c>). Both the customer and the code
/// are unique: the first decides that two first reads at once share one code, the second that a
/// drawn code nobody else holds is the only kind saved.
/// </summary>
internal sealed class ReferralCodeConfiguration : EntityConfiguration<ReferralCode>
{
    public const string CustomerIndex = "ux_referral_codes_customer";
    public const string CodeIndex = "ux_referral_codes_code";

    protected override void ConfigureEntity(EntityTypeBuilder<ReferralCode> builder)
    {
        builder.ToTable("referral_codes");

        builder.Property(code => code.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(code => code.Code).HasMaxLength(ReferralCode.Length).IsRequired();

        builder.HasIndex(code => code.CustomerId).IsUnique().HasDatabaseName(CustomerIndex);
        builder.HasIndex(code => code.Code).IsUnique().HasDatabaseName(CodeIndex);

        builder.HasOne<Customer>().WithMany().HasForeignKey(code => code.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Referrals (§ 39). A customer is referred once: <c>friend_customer_id</c> is unique. A bill pays
/// one referral at most: <c>reward_invoice_id</c> is unique when set. The checks keep the status,
/// the reason and the times consistent, so a half-written reward cannot be saved.
/// </summary>
internal sealed class ReferralConfiguration : EntityConfiguration<Referral>
{
    public const string FriendIndex = "ux_referrals_friend";

    protected override void ConfigureEntity(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("referrals", table =>
        {
            table.HasCheckConstraint(
                "ck_referrals_status",
                "(status = 'Pending' AND reject_reason IS NULL AND rewarded_at IS NULL AND rejected_at IS NULL) "
                + "OR (status = 'Rewarded' AND reject_reason IS NULL AND rewarded_at IS NOT NULL AND reward_invoice_id IS NOT NULL) "
                + "OR (status = 'Rejected' AND reject_reason IS NOT NULL AND rejected_at IS NOT NULL AND rewarded_at IS NULL)");
            table.HasCheckConstraint("ck_referrals_points", "referrer_points >= 0 AND friend_points >= 0");
            table.HasCheckConstraint("ck_referrals_not_self_reward", "status <> 'Rewarded' OR referrer_customer_id <> friend_customer_id");
        });

        builder.Property(referral => referral.ReferrerCustomerId).HasMaxLength(40).IsRequired();
        builder.Property(referral => referral.FriendCustomerId).HasMaxLength(40).IsRequired();
        builder.Property(referral => referral.Code).HasMaxLength(ReferralCode.Length).IsRequired();
        builder.Property(referral => referral.Channel).HasMaxLength(20).IsRequired();
        builder.Property(referral => referral.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(referral => referral.RejectReason).HasMaxLength(40);
        builder.Property(referral => referral.RewardInvoiceId).HasMaxLength(40);

        builder.HasIndex(referral => referral.FriendCustomerId).IsUnique().HasDatabaseName(FriendIndex);
        builder.HasIndex(referral => referral.RewardInvoiceId).IsUnique().HasFilter("reward_invoice_id IS NOT NULL");
        builder.HasIndex(referral => new { referral.ReferrerCustomerId, referral.RewardedAt });
        builder.HasIndex(referral => new { referral.CreatedAt, referral.Id });

        builder.HasOne<Customer>().WithMany().HasForeignKey(referral => referral.ReferrerCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(referral => referral.FriendCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(referral => referral.RewardInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
