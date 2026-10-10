namespace HappyPaws.Domain.Reviews;

/// <summary>
/// Where a customer review stands (<c>docs/contract-extensions.md § 36</c>).
/// <para>
/// Every review starts <see cref="Pending"/> and stays off the public site until a member of staff
/// publishes or rejects it. <see cref="Published"/> may still be rejected later, which is how a
/// customer's request to take their review down is honoured. <see cref="Rejected"/> is final.
/// </para>
/// </summary>
public enum ReviewStatus
{
    /// <summary>Written by the customer, waiting for staff. Never shown publicly.</summary>
    Pending,

    /// <summary>On the public site and counted in the average.</summary>
    Published,

    /// <summary>Taken off, or never put on, for one of the listed reasons. Final.</summary>
    Rejected,
}

/// <summary>How the review reached the clinic.</summary>
public enum ReviewSource
{
    /// <summary>Written in the portal by the signed-in customer.</summary>
    Portal,

    /// <summary>Written through a one-time invitation link, by a customer without a portal account.</summary>
    Invitation,
}

/// <summary>What name the public page shows for the reviewer: the customer picks.</summary>
public enum ReviewNameChoice
{
    /// <summary>The first word of their name, without a title such as คุณ.</summary>
    FirstName,

    /// <summary>"ลูกค้า", nothing else.</summary>
    Anonymous,
}
