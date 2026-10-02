using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Customers;

/// <summary>
/// What a customer has agreed to. The purposes are kept apart because they are agreed to
/// separately and withdrawn separately.
/// </summary>
public enum ConsentPurpose
{
    /// <summary>
    /// Messages about the pet's own care — reminders, queue calls, the visit summary. Granted by
    /// default: the owner gave the clinic a phone number in order to be contacted about their
    /// animal, and treating that as unconsented would silence the appointment reminder.
    /// </summary>
    ServiceMessages,

    /// <summary>Promotions, campaigns, newsletters. <b>Denied</b> until explicitly granted.</summary>
    Marketing,

    /// <summary>Sending clinical records to a referral clinic or an insurer. Denied by default.</summary>
    MedicalRecordSharing,

    /// <summary>
    /// The base agreement to hold personal and health data beyond what delivering care requires —
    /// research, aggregate statistics, retention past the clinical need. Denied by default.
    /// </summary>
    ExtendedDataProcessing,
}

/// <summary>Where a consent decision was taken.</summary>
public enum ConsentSource
{
    /// <summary>The customer changed it themselves in the portal.</summary>
    Portal,

    /// <summary>Recorded at the counter from a paper form or a spoken instruction.</summary>
    Counter,
}

/// <summary>
/// One consent decision, recorded at a moment in time.
/// <para>
/// <b>Append-only.</b> Every grant and every withdrawal is a new row; the current position is the
/// most recent row for that purpose. A mutable flag would answer "may we message them today" and
/// nothing else — and the question a data-protection complaint actually asks is "on what date did
/// they agree, and when did they take it back". A column cannot answer that.
/// </para>
/// <para>
/// <b>This entity is not in the API reference.</b> The contract has no consent, retention or
/// erasure endpoint anywhere, while the system it describes holds identifiable personal data and
/// animal health data in Thailand. See <c>docs/contract-extensions.md § 4</c>.
/// </para>
/// </summary>
public sealed class CustomerConsent : Entity
{
    private CustomerConsent()
    {
    }

    public string CustomerId { get; private set; } = string.Empty;

    public ConsentPurpose Purpose { get; private set; }

    public bool Granted { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public ConsentSource Source { get; private set; }

    /// <summary>The staff account that recorded it, when it came from the counter.</summary>
    public string? RecordedByUserId { get; private set; }

    /// <summary>
    /// Which version of the privacy notice was shown. Consent is to a specific statement of what
    /// the data will be used for; without this the record says someone agreed to something
    /// unspecified.
    /// </summary>
    public string? PolicyVersion { get; private set; }

    public string? Note { get; private set; }

    public static CustomerConsent Record(
        string customerId,
        ConsentPurpose purpose,
        bool granted,
        ConsentSource source,
        DateTimeOffset recordedAt,
        string? recordedByUserId = null,
        string? policyVersion = null,
        string? note = null)
    {
        if (source == ConsentSource.Counter && string.IsNullOrWhiteSpace(recordedByUserId))
        {
            // A consent recorded on someone's behalf with no record of who recorded it is not
            // evidence of anything.
            throw new DomainException("invalid_consent", "ต้องระบุผู้บันทึกเมื่อบันทึกความยินยอมแทนลูกค้า");
        }

        return new CustomerConsent
        {
            Id = EntityId.New(EntityId.Consent),
            CustomerId = customerId,
            Purpose = purpose,
            Granted = granted,
            Source = source,
            RecordedAt = recordedAt,
            RecordedByUserId = recordedByUserId,
            PolicyVersion = policyVersion,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }

    /// <summary>
    /// What a purpose defaults to when the customer has never been asked.
    /// <para>
    /// Only care-related messaging is on. Everything else is off until someone says otherwise,
    /// which is the only defensible direction for a default to point.
    /// </para>
    /// </summary>
    public static bool DefaultFor(ConsentPurpose purpose) => purpose == ConsentPurpose.ServiceMessages;
}
