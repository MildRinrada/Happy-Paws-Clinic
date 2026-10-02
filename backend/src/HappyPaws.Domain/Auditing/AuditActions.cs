namespace HappyPaws.Domain.Auditing;

/// <summary>
/// The explicit list of actions this platform audits.
/// <para>
/// <b>This list is the answer to a gap in the API contract.</b> The reference gives two endpoints
/// that read audit logs — <c>GET /staff/audit-logs</c> and <c>GET /staff/audit-logs/access</c> —
/// but never states which actions must be written. Without a named list, "audited" degrades into
/// whatever each module happened to remember, which is indistinguishable from not being audited at
/// all. So the list lives here, in one file, and is part of the contract this codebase keeps. See
/// <c>docs/contract-extensions.md § 5</c>.
/// </para>
/// <para>
/// Two rules govern additions. An action belongs here if it <b>touches personal or health data</b>,
/// or if it <b>changes who can do what</b>. An action does not belong here merely because it was
/// interesting to a developer — the log is read during an incident, and volume is what makes a log
/// unreadable.
/// </para>
/// <para>
/// This list is the <b>requirement</b>, not an inventory of today's call sites: several entries
/// name endpoints that are not built yet, and they stay here so that building one without its audit
/// row is visibly an omission.
/// </para>
/// <para>
/// <b>Two omissions are deliberate</b>, and both are here so they read as decisions rather than
/// oversights. Autosaving a consultation draft writes nothing: a row every ten seconds per open
/// case would bury everything that matters, and the signed record plus its amendments already say
/// what the case ended up saying. Searching or opening a <i>customer</i> record writes nothing
/// either — a receptionist looks up dozens of people an hour, and logging that would drown
/// <c>record.viewed</c>, which is the row the access log exists for. The line is drawn at health
/// data: reading a patient's file or timeline is audited, reading a phone number is not.
/// </para>
/// </summary>
public static class AuditActions
{
    // ---- Access to health data (the "access log" the contract reads separately) --------------

    /// <summary>A signed medical record was opened. The single most important row in this table.</summary>
    public const string RecordViewed = "record.viewed";

    /// <summary>A pet's clinical history was listed.</summary>
    public const string PetHistoryViewed = "pet.history_viewed";

    /// <summary>An open consultation was read by someone other than its author.</summary>
    public const string ConsultationViewed = "consultation.viewed";

    /// <summary>One patient's medication history was listed — part of the file, like the timeline.</summary>
    public const string PrescriptionsViewed = "pet.prescriptions_viewed";

    /// <summary>One patient's lab requests and results were listed.</summary>
    public const string LabResultsViewed = "pet.lab_results_viewed";

    /// <summary>
    /// A certificate, referral letter or vaccination book was printed from a patient's file. A copy
    /// of health data left the system on paper, which is why it is in the access log.
    /// </summary>
    public const string DocumentGenerated = "pet.document_generated";

    // ---- Changes to health data ---------------------------------------------------------------

    public const string ConsultationOpened = "consultation.opened";
    public const string ConsultationClosed = "consultation.closed";
    public const string VitalsRecorded = "consultation.vitals_recorded";
    public const string ExaminationRecorded = "consultation.examination_recorded";
    public const string DiagnosisAdded = "consultation.diagnosis_added";
    public const string TreatmentAdded = "consultation.treatment_added";
    public const string PrescriptionAdded = "consultation.prescription_added";

    /// <summary>A prescription was written over a recorded allergy, with the vet's acknowledgement.</summary>
    public const string AllergyOverridden = "consultation.allergy_overridden";

    public const string RecordSigned = "record.signed";
    public const string RecordAmended = "record.amended";

    public const string AllergyRecorded = "pet.allergy_recorded";
    public const string PetMarkedDeceased = "pet.marked_deceased";
    public const string PetCreated = "pet.created";

    /// <summary>A dose was given. Carries the lot, which is what a recall searches by.</summary>
    public const string VaccinationRecorded = "pet.vaccination_recorded";

    /// <summary>A course of treatment was planned, and usually its visits booked with it.</summary>
    public const string TreatmentPlanCreated = "pet.treatment_plan_created";

    /// <summary>A file was attached to a patient's record, by the clinic or by the owner.</summary>
    public const string DocumentAdded = "pet.document_added";

    public const string LabRequested = "lab.requested";
    public const string LabResultsRecorded = "lab.results_recorded";

    /// <summary>A vet read a set of results. "Who saw the abnormal value, and when" is the question it answers.</summary>
    public const string LabResultsReviewed = "lab.results_reviewed";

    /// <summary>A prescription left the pharmacy. The written drug became a drug in the owner's hand.</summary>
    public const string PrescriptionDispensed = "prescription.dispensed";

    // ---- Personal data and consent ------------------------------------------------------------

    /// <summary>A person's record was created. Who entered it is the first question of any dispute.</summary>
    public const string CustomerCreated = "customer.created";

    /// <summary>
    /// Two records were found to be one person and folded together. Audited without hesitation: it
    /// moves patients, clinical history and money between records and the API cannot undo it.
    /// </summary>
    public const string CustomerMerged = "customer.merged";

    public const string ConsentRecorded = "consent.recorded";
    public const string ConsentWithdrawn = "consent.withdrawn";
    public const string DataRequestRaised = "data_request.raised";
    public const string DataRequestClosed = "data_request.closed";

    // ---- Actions that change who can do what --------------------------------------------------

    public const string SignedIn = "auth.signed_in";
    public const string SignInFailed = "auth.sign_in_failed";
    public const string PasswordChanged = "auth.password_changed";

    /// <summary>A customer opened a portal account with a phone number they proved with a code.</summary>
    public const string AccountRegistered = "auth.registered";

    /// <summary>A password was set from a reset link rather than by someone who knew the old one.</summary>
    public const string PasswordReset = "auth.password_reset";
    public const string SessionRevoked = "auth.session_revoked";
    public const string PetAccessGranted = "pet.access_granted";
    public const string PetAccessRevoked = "pet.access_revoked";

    /// <summary>A customer was invited to open a portal account — a new way into this clinic's data.</summary>
    public const string CustomerInvited = "customer.invited";

    /// <summary>
    /// The first owner account was created from the server's own command line. The one entry with
    /// no actor: nobody could be signed in yet, which is exactly why it has to be on record.
    /// </summary>
    public const string ClinicBootstrapped = "clinic.bootstrapped";

    /// <summary>An operator re-issued an account's password and second factor from the command line.</summary>
    public const string CredentialsReset = "auth.credentials_reset";

    public const string StaffInvited = "staff.invited";

    /// <summary>An invitation became a login. Written without an actor — the invitee has no token yet.</summary>
    public const string StaffInvitationAccepted = "staff.invitation_accepted";

    /// <summary>A member's position, roles, branches or licence changed.</summary>
    public const string StaffUpdated = "staff.updated";

    public const string StaffDeactivated = "staff.deactivated";

    /// <summary>
    /// What the public website says about a member changed, including whether it lists them at all.
    /// Audited because it publishes a person's name and face.
    /// </summary>
    public const string StaffPublicProfileUpdated = "staff.public_profile_updated";

    /// <summary>The operator's <c>seed-staff</c> command created sample staff accounts. Written without an actor.</summary>
    public const string StaffSeeded = "staff.seeded";

    /// <summary>
    /// An account was suspended, deactivated or reactivated by someone else. Who can sign in at all
    /// changed, so it is on record with the before and after.
    /// </summary>
    public const string UserStatusChanged = "user.status_changed";

    /// <summary>Someone changed their own name, email, phone, language or notification choices.</summary>
    public const string AccountProfileUpdated = "account.profile_updated";

    /// <summary>An account set up or replaced its own second factor.</summary>
    public const string TwoFactorEnrolled = "auth.two_factor_enrolled";

    /// <summary>
    /// A sign-in used a recovery code instead of the authenticator app. Rare by design, so worth
    /// noticing: it is also what someone holding a stolen code would do.
    /// </summary>
    public const string RecoveryCodeUsed = "auth.recovery_code_used";

    // ---- The roster: who is in, when ------------------------------------------------------------

    /// <summary>Shifts were added to the roster. Changes what can be booked.</summary>
    public const string ShiftsCreated = "schedule.shifts_created";

    /// <summary>Time off was recorded, taking time out of shifts.</summary>
    public const string TimeOffRecorded = "schedule.time_off_recorded";

    /// <summary>The rules that hand out cases automatically were replaced.</summary>
    public const string AssignmentRulesReplaced = "schedule.assignment_rules_replaced";

    public const string RoleCreated = "role.created";

    /// <summary>
    /// A role's scope set was replaced. Everyone holding the role changed what they can do in one
    /// request, so the before and after are both in the details.
    /// </summary>
    public const string RolePermissionsReplaced = "role.permissions_replaced";

    /// <summary>
    /// The clinic changed what a signed record must contain or who may sign one. Audited because
    /// it moves the line between a record that could be signed yesterday and one that cannot today.
    /// </summary>
    public const string MedicalRecordSettingsChanged = "settings.medical_record_changed";

    // ---- Clinical operations with a money or duty-of-care consequence -------------------------

    public const string AppointmentCancelled = "appointment.cancelled";

    /// <summary>A customer did not come. A strike under the cancellation policy, which can carry a fee.</summary>
    public const string AppointmentNoShow = "appointment.no_show";

    /// <summary>The cutoff, the fee or the allowance changed — what a customer can be charged for.</summary>
    public const string CancellationPolicyChanged = "settings.cancellation_policy_changed";

    public const string QueuePriorityChanged = "queue.priority_changed";

    /// <summary>A bill was raised, by hand or by closing a case.</summary>
    public const string InvoiceIssued = "invoice.issued";

    /// <summary>A bill was cancelled before anything was collected against it.</summary>
    public const string InvoiceVoided = "invoice.voided";

    /// <summary>
    /// A reduction was approved. The one entry here that exists for the approver's sake rather than
    /// the patient's: "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ" is only enforceable if the pair is recorded
    /// somewhere a supervisor reads.
    /// </summary>
    public const string InvoiceDiscounted = "invoice.discounted";

    public const string PaymentCollected = "payment.collected";
    public const string PaymentRefunded = "payment.refunded";

    /// <summary>A receipt or ใบกำกับภาษี was issued. A tax document, so who issued it matters.</summary>
    public const string ReceiptIssued = "receipt.issued";

    /// <summary>A service's price list was set or scheduled. Who changed what a visit costs, and from when.</summary>
    public const string ServicePricingChanged = "service.pricing_changed";

    /// <summary>
    /// The subset read by <c>GET /staff/audit-logs/access</c> — "การเปิดดูเวชระเบียนของคนไข้แต่ละราย".
    /// <para>
    /// Reads are separated from writes because they answer a different question. "Who changed this
    /// record" is a clinical-governance question; "who looked at this patient's file" is a privacy
    /// question, and it is the one a data subject is entitled to ask.
    /// </para>
    /// </summary>
    public static readonly IReadOnlyList<string> AccessActions =
    [
        RecordViewed,
        PetHistoryViewed,
        ConsultationViewed,
        PrescriptionsViewed,
        LabResultsViewed,
        DocumentGenerated,
    ];

    /// <summary>Every action this platform is expected to write. Asserted against in a unit test.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        RecordViewed, PetHistoryViewed, ConsultationViewed, PrescriptionsViewed, LabResultsViewed, DocumentGenerated,
        ConsultationOpened, ConsultationClosed,
        VitalsRecorded, ExaminationRecorded, DiagnosisAdded, TreatmentAdded,
        PrescriptionAdded, AllergyOverridden,
        RecordSigned, RecordAmended,
        AllergyRecorded, PetMarkedDeceased, PetCreated, VaccinationRecorded, TreatmentPlanCreated, DocumentAdded,
        LabRequested, LabResultsRecorded, LabResultsReviewed, PrescriptionDispensed,
        CustomerCreated, CustomerMerged,
        ConsentRecorded, ConsentWithdrawn, DataRequestRaised, DataRequestClosed,
        SignedIn, SignInFailed, PasswordChanged, SessionRevoked, AccountRegistered, PasswordReset,
        PetAccessGranted, PetAccessRevoked, CustomerInvited,
        ClinicBootstrapped, CredentialsReset,
        StaffInvited, StaffInvitationAccepted, StaffUpdated, StaffDeactivated,
        StaffPublicProfileUpdated, StaffSeeded,
        RoleCreated, RolePermissionsReplaced, MedicalRecordSettingsChanged,
        AppointmentCancelled, AppointmentNoShow, CancellationPolicyChanged, QueuePriorityChanged,
        InvoiceIssued, InvoiceVoided, InvoiceDiscounted,
        PaymentCollected, PaymentRefunded, ReceiptIssued, ServicePricingChanged,
        ShiftsCreated, TimeOffRecorded, AssignmentRulesReplaced,
        AccountProfileUpdated, TwoFactorEnrolled, RecoveryCodeUsed, UserStatusChanged,
    };

    public static bool IsKnown(string action) => All.Contains(action);

    public static bool IsAccess(string action) => AccessActions.Contains(action);
}

/// <summary>The kinds of thing an audit row can point at. Kept short on purpose.</summary>
public static class AuditResources
{
    public const string MedicalRecord = "medical_record";
    public const string Consultation = "consultation";
    public const string Pet = "pet";
    public const string Customer = "customer";
    public const string Appointment = "appointment";
    public const string QueueTicket = "queue_ticket";
    public const string UserAccount = "user_account";
    public const string Consent = "consent";
    public const string DataRequest = "data_request";
    public const string Invoice = "invoice";
    public const string StaffMember = "staff_member";
    public const string StaffInvitation = "staff_invitation";
    public const string Role = "role";
    public const string Service = "service";
    public const string Settings = "settings";
    public const string Shift = "shift";
    public const string TimeOff = "time_off";
    public const string Vaccination = "vaccination";
    public const string LabRequest = "lab_request";
    public const string Prescription = "prescription";
}
