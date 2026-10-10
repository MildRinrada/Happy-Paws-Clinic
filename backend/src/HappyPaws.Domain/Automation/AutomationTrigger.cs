namespace HappyPaws.Domain.Automation;

/// <summary>
/// What a reminder rule watches. Each one reads a date the clinic already keeps; nothing here is
/// a second copy of it.
/// </summary>
public enum AutomationTrigger
{
    /// <summary>A vaccine's next dose falls due (<c>pet_vaccinations.next_due_on</c> of the latest dose of each vaccine).</summary>
    VaccineDue,

    /// <summary>A booked or confirmed appointment is coming up ("reminder ตั้งไว้ T-24h และ T-2h").</summary>
    AppointmentUpcoming,

    /// <summary>A session of a treatment plan is coming up, booked or not yet booked.</summary>
    TreatmentPlanSession,

    /// <summary>
    /// A customer is later back than their own usual gap between visits ("คิดถึงน้อง"): the lapsed
    /// rule of <c>docs/contract-extensions.md § 33</c>, the same one <c>GET /staff/customers/due-back</c>
    /// reads. A message nobody asked for, so it goes out under marketing consent, not care.
    /// </summary>
    CustomerLapsed,

    /// <summary>
    /// A visit is over (a closed consultation or a bill that was not voided, for a pet) and has not
    /// been reviewed: the review invitation of <c>docs/contract-extensions.md § 36</c>, sent a few
    /// days after. Asking for a review is marketing, so it goes out under marketing consent.
    /// </summary>
    VisitCompleted,
}

/// <summary>Where a rule's message goes. <see cref="Auto"/> lets the router try LINE, then SMS, then email.</summary>
public enum AutomationChannel
{
    Auto,
    Line,
    Sms,
    Email,
}

/// <summary>The kind of record a reminder is about, half of its "already sent" key.</summary>
public enum AutomationSubjectKind
{
    Vaccination,
    Appointment,
    TreatmentPlanSession,

    /// <summary>The customer themselves, for <see cref="AutomationTrigger.CustomerLapsed"/>.</summary>
    Customer,

    /// <summary>One visit, named by its consultation or bill, for <see cref="AutomationTrigger.VisitCompleted"/>.</summary>
    Visit,
}
