namespace HappyPaws.Domain.Consultations;

/// <summary>
/// A consultation is open or it is finished. There is no third state.
/// <para>
/// The contract's own state machine belongs to the <i>medical record</i> —
/// <c>draft → signed → amended</c> — not to the consultation. A consultation is the working
/// surface a vet types into; the record is what it produces. Keeping them as two types is what
/// makes "autosave every ten seconds" and "append-only after signing" both true at once.
/// </para>
/// </summary>
public enum ConsultationStatus
{
    /// <summary>Open. Editable, autosaved, not yet part of the patient's permanent history.</summary>
    Draft,

    /// <summary>Closed by <c>POST /staff/consultations/{id}/close</c>, which signs the record.</summary>
    Closed,
}
