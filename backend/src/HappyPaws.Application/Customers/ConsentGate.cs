using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers;

/// <summary>
/// Decides whether the clinic may send a customer a given message.
/// <para>
/// This is what makes consent more than a stored preference. Without it the toggles in the portal
/// would be decoration — the clinic would keep messaging, and the record of consent would be a
/// record of a promise nobody kept.
/// </para>
/// </summary>
public sealed class ConsentGate(IConsentRepository consents)
{
    /// <summary>
    /// Which purpose a notification template falls under.
    /// <para>
    /// Explicit rather than derived from the template name. A new template whose name happens not
    /// to match a pattern would otherwise default into whichever bucket the string comparison put
    /// it in, and the failure mode of that mistake is marketing sent to someone who opted out.
    /// An unlisted template is treated as marketing — the restrictive side of the error.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, ConsentPurpose> Purposes = new(StringComparer.Ordinal)
    {
        ["queue.called"] = ConsentPurpose.ServiceMessages,
        ["consultation.summary"] = ConsentPurpose.ServiceMessages,
        ["pet.deceased_appointments_cancelled"] = ConsentPurpose.ServiceMessages,

        // A receipt is a record of a transaction the customer took part in, not a message the
        // clinic chose to send them. Withholding it because someone declined marketing would
        // suppress a tax document.
        ["receipt.issued"] = ConsentPurpose.ServiceMessages,

        // An invitation to open a portal account is about the care relationship the customer
        // already has — the account is how they see their own pet's appointments and bills. It is
        // sent once per invitation, on a counter action, never as a campaign.
        ["customer.portal_invitation"] = ConsentPurpose.ServiceMessages,

        // The customer asked to be told. A message they requested about a visit they want is a
        // service message, not a campaign.
        ["waiting_list.slot_available"] = ConsentPurpose.ServiceMessages,
    };

    public static ConsentPurpose PurposeOf(string templateKey) =>
        Purposes.TryGetValue(templateKey, out var purpose) ? purpose : ConsentPurpose.Marketing;

    /// <summary>True when this customer may be sent this template.</summary>
    public async Task<bool> MayReceiveAsync(
        string customerId,
        string templateKey,
        CancellationToken cancellationToken = default)
    {
        var purpose = PurposeOf(templateKey);
        var current = await consents.CurrentAsync(customerId, cancellationToken);

        return current.TryGetValue(purpose, out var recorded) && recorded is not null
            ? recorded.Granted
            : CustomerConsent.DefaultFor(purpose);
    }
}
