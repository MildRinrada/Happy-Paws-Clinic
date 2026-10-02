using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Customers;

/// <summary>
/// An outstanding invitation to open a portal account —
/// <c>POST /staff/customers/{id}/invite</c>, "ชวนให้เปิดใช้ portal และผูก LINE".
/// <para>
/// <b>There is no token here, and that is deliberate.</b> The obvious design is a signed link the
/// customer clicks, but this contract's customer sign-up is
/// <c>POST /auth/otp/request</c> → <c>POST /auth/register</c> — an OTP to the phone number the
/// clinic already holds. A token would be a second, weaker credential for the same act: something
/// forwardable, loggable and screenshot-able, standing next to a one-time code that is none of
/// those. So the invitation records that the clinic asked, when, and by whom; the customer proves
/// who they are the way the contract already says they do.
/// </para>
/// <para>
/// It is a row rather than an audit entry because it has to be <i>read back</i>: "have we already
/// invited this person, and is that invitation still live" is a question the counter asks, and an
/// audit log is not a working set.
/// </para>
/// <para>
/// The "ผูก LINE" half of that description is out of scope by a standing decision — LINE is an
/// outbound channel here, never an identity. See <c>docs/assumptions.md § A</c>.
/// </para>
/// </summary>
public sealed class CustomerInvitation : Entity
{
    /// <summary>How long an unanswered invitation stays live before it has to be sent again.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private CustomerInvitation()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    /// <summary>The number or address the invitation went to, as it stood when it was sent.</summary>
    public string SentTo { get; private set; } = string.Empty;

    public string InvitedByStaffId { get; private set; } = string.Empty;

    public DateTimeOffset SentAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Set when the customer record gains a portal account.</summary>
    public DateTimeOffset? AcceptedAt { get; private set; }

    public bool IsAccepted => AcceptedAt is not null;

    public bool IsLiveAt(DateTimeOffset now) => !IsAccepted && now < ExpiresAt;

    public static CustomerInvitation Send(
        string customerId,
        string sentTo,
        string invitedByStaffId,
        DateTimeOffset now) =>
        new()
        {
            Id = EntityId.New(EntityId.Invitation),
            CustomerId = customerId,
            SentTo = sentTo,
            InvitedByStaffId = invitedByStaffId,
            SentAt = now,
            ExpiresAt = now + Lifetime,
        };

    /// <summary>Re-sending a live invitation refreshes it rather than stacking a second row.</summary>
    public void Resend(string sentTo, string invitedByStaffId, DateTimeOffset now)
    {
        if (IsAccepted)
        {
            throw new DomainException("already_accepted", "ลูกค้ารายนี้เปิดใช้ portal แล้ว");
        }

        SentTo = sentTo;
        InvitedByStaffId = invitedByStaffId;
        SentAt = now;
        ExpiresAt = now + Lifetime;
    }

    public void Accept(DateTimeOffset now) => AcceptedAt ??= now;
}
