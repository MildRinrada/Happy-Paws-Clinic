namespace HappyPaws.Domain.Consultations;

/// <summary>
/// The parts of a case a clinic may insist on before it is signed — the "ฟิลด์บังคับ" half of
/// <c>PATCH /clinic/settings/medical-record</c>.
/// <para>
/// A diagnosis is not on this list because it is not optional: <see cref="Consultation.Close"/>
/// refuses a case without one whatever the clinic has configured. These are the fields a vet may
/// legitimately leave blank unless their clinic has decided otherwise.
/// </para>
/// <para>
/// Names are the wire names, so the setting, the refusal's <c>details.missing_fields</c> and the
/// request body that fixes it all use the same word.
/// </para>
/// </summary>
public static class ConsultationRequirements
{
    public const string ChiefComplaint = "chief_complaint";
    public const string Subjective = "subjective";
    public const string Objective = "objective";
    public const string Assessment = "assessment";
    public const string Plan = "plan";
    public const string Vitals = "vitals";
    public const string Examination = "examination";

    public static readonly IReadOnlyList<string> All =
    [
        ChiefComplaint, Subjective, Objective, Assessment, Plan, Vitals, Examination,
    ];

    /// <summary>The required parts this case has not filled in, in the order they were asked for.</summary>
    public static IReadOnlyList<string> MissingFrom(Consultation consultation, IEnumerable<string> required) =>
        [.. required.Where(field => !IsPresent(consultation, field))];

    private static bool IsPresent(Consultation consultation, string field) => field switch
    {
        ChiefComplaint => consultation.ChiefComplaint is not null,
        Subjective => consultation.Subjective is not null,
        Objective => consultation.Objective is not null,
        Assessment => consultation.Assessment is not null,
        Plan => consultation.Plan is not null,
        Vitals => consultation.Vitals.Count > 0,
        Examination => consultation.Findings.Count > 0,

        // A name this build does not know cannot be checked, so it cannot be missing. Settings
        // validate against All on the way in; this only matters across a downgrade.
        _ => true,
    };
}
