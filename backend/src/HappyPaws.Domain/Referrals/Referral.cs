using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Referrals;

/// <summary>Where a referral stands.</summary>
public enum ReferralStatus
{
    /// <summary>Recorded when the friend signed up; waiting for their first paid bill.</summary>
    Pending,

    /// <summary>The friend's first bill was paid and the points were given.</summary>
    Rewarded,

    /// <summary>Recorded, but it does not earn anything. <see cref="Referral.RejectReason"/> says why.</summary>
    Rejected,
}

/// <summary>Why a referral earns nothing. Stored and sent as these strings.</summary>
public static class ReferralRejectReasons
{
    /// <summary>The code is the friend's own.</summary>
    public const string SelfReferral = "self_referral";

    /// <summary>The friend was already a customer: an existing login, or a record the counter had made before.</summary>
    public const string NotNewCustomer = "not_new_customer";

    /// <summary>The friend shares a phone number or an email address with the person who invited them.</summary>
    public const string SharedContact = "shared_contact";

    /// <summary>The clinic had switched referral rewards off when the friend's first bill was paid.</summary>
    public const string ProgrammeOff = "programme_off";

    public static readonly IReadOnlyList<string> All = [SelfReferral, NotNewCustomer, SharedContact, ProgrammeOff];
}

/// <summary>How the friend arrived with the code.</summary>
public static class ReferralChannels
{
    /// <summary><c>POST /booking/guest/verify</c>, through the website's booking page.</summary>
    public const string GuestBooking = "guest_booking";

    /// <summary><c>POST /auth/register</c>.</summary>
    public const string Register = "register";
}

/// <summary>
/// A friend who signed up with someone's referral code, and what it earned
/// (<c>docs/contract-extensions.md § 39</c>).
/// <para>
/// <b>One per friend.</b> A customer can be referred once, ever: the friend's id is unique. The row
/// is written when the friend signs up, as <see cref="ReferralStatus.Pending"/>, or straight away as
/// <see cref="ReferralStatus.Rejected"/> when the sign-up does not qualify, so the clinic can see
/// codes being tried.
/// </para>
/// <para>
/// <b>Rewarded once.</b> The friend's first bill that reaches <c>paid</c> with money on it turns a
/// pending referral into <see cref="ReferralStatus.Rewarded"/> and puts the points in both ledgers.
/// The points are kept on the row, so the referrer's monthly cap and the portal's totals are read
/// from here, not worked out again from settings that may have changed since.
/// </para>
/// </summary>
public sealed class Referral : Entity
{
    private Referral()
    {
    }

    /// <summary>The customer whose code was used.</summary>
    public string ReferrerCustomerId { get; private set; } = string.Empty;

    /// <summary>The customer who signed up with it.</summary>
    public string FriendCustomerId { get; private set; } = string.Empty;

    /// <summary>The code as it was used.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>See <see cref="ReferralChannels"/>.</summary>
    public string Channel { get; private set; } = string.Empty;

    public ReferralStatus Status { get; private set; }

    /// <summary>One of <see cref="ReferralRejectReasons"/> when rejected, otherwise null.</summary>
    public string? RejectReason { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    /// <summary>What the person who invited got: zero when they were over the month's cap.</summary>
    public int ReferrerPoints { get; private set; }

    /// <summary>What the friend got.</summary>
    public int FriendPoints { get; private set; }

    /// <summary>True when the person who invited had already been rewarded the month's cap.</summary>
    public bool OverMonthlyCap { get; private set; }

    /// <summary>The friend's bill whose payment earned the reward.</summary>
    public string? RewardInvoiceId { get; private set; }

    public DateTimeOffset? RewardedAt { get; private set; }

    /// <summary>
    /// Records a sign-up with a code. <paramref name="rejectReason"/> comes from
    /// <see cref="ReferralRules.Judge"/>: null makes it pending, a reason rejects it now.
    /// </summary>
    public static Referral Record(
        string referrerCustomerId,
        string friendCustomerId,
        string code,
        string channel,
        string? rejectReason,
        DateTimeOffset now)
    {
        if (rejectReason is not null && !ReferralRejectReasons.All.Contains(rejectReason))
        {
            throw new ArgumentOutOfRangeException(nameof(rejectReason), rejectReason, null);
        }

        return new Referral
        {
            Id = EntityId.New(EntityId.Referral),
            ReferrerCustomerId = referrerCustomerId,
            FriendCustomerId = friendCustomerId,
            Code = code,
            Channel = channel,
            Status = rejectReason is null ? ReferralStatus.Pending : ReferralStatus.Rejected,
            RejectReason = rejectReason,
            RejectedAt = rejectReason is null ? null : now,
        };
    }

    /// <summary>The friend's first paid bill. Only a pending referral can be rewarded.</summary>
    public void Reward(string invoiceId, int referrerPoints, int friendPoints, bool overMonthlyCap, DateTimeOffset now)
    {
        RequirePending();

        if (referrerPoints < 0 || friendPoints < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(referrerPoints));
        }

        Status = ReferralStatus.Rewarded;
        RewardInvoiceId = invoiceId;
        ReferrerPoints = referrerPoints;
        FriendPoints = friendPoints;
        OverMonthlyCap = overMonthlyCap;
        RewardedAt = now;
    }

    /// <summary>Closes a pending referral without a reward, for a reason from <see cref="ReferralRejectReasons"/>.</summary>
    public void Reject(string reason, DateTimeOffset now)
    {
        RequirePending();

        if (!ReferralRejectReasons.All.Contains(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
        }

        Status = ReferralStatus.Rejected;
        RejectReason = reason;
        RejectedAt = now;
    }

    private void RequirePending()
    {
        if (Status != ReferralStatus.Pending)
        {
            throw new DomainException("state_conflict", "การชวนนี้ได้ข้อสรุปไปแล้ว");
        }
    }
}
