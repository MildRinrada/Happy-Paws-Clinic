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

    /// <summary>
    /// Staff corrected a patient's identity card. Carries each changed field before and after.
    /// Not in the API reference; see <c>docs/contract-extensions.md § 28</c>.
    /// </summary>
    public const string PetUpdated = "pet.updated";

    /// <summary>
    /// A death recorded by mistake was taken back, with the reason. Not in the API reference; see
    /// <c>docs/contract-extensions.md § 29</c>.
    /// </summary>
    public const string PetDeceasedUndone = "pet.deceased_undone";

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

    /// <summary>
    /// The counter saved a LINE address on a customer's record, picked from the recent followers
    /// (<c>docs/contract-extensions.md § 18</c>). Where the clinic's messages to a person go is
    /// personal data, and a wrong pick sends their pet's news to someone else.
    /// </summary>
    public const string CustomerLineLinked = "customer.line_linked";

    /// <summary>The counter took a LINE address off a customer's record.</summary>
    public const string CustomerLineUnlinked = "customer.line_unlinked";

    public const string ConsentRecorded = "consent.recorded";
    public const string ConsentWithdrawn = "consent.withdrawn";
    public const string DataRequestRaised = "data_request.raised";
    public const string DataRequestClosed = "data_request.closed";

    /// <summary>
    /// An announcement went out to a list of customers on LINE. Personal data used for a campaign,
    /// so who sent it, to how many, and how many it actually reached are on record.
    /// </summary>
    public const string NotificationBroadcast = "notification.broadcast";

    /// <summary>
    /// The clinic's LINE channel credentials or switches changed. Whoever holds the access token can
    /// message every follower in the clinic's name, so a change to it is recorded (never its value).
    /// </summary>
    public const string LineIntegrationConfigured = "integration.line_configured";

    /// <summary>
    /// An outbound webhook was created. From then on events about appointments, bills and pets
    /// leave the system for that URL, so who pointed them there is on record (never the secret).
    /// </summary>
    public const string WebhookCreated = "integration.webhook_created";

    /// <summary>
    /// An API key was issued. It acts with its scopes until it expires, without a person signing
    /// in, so who made it, with which scopes, for which branches and until when is on record.
    /// </summary>
    public const string ApiKeyCreated = "integration.api_key_created";

    /// <summary>The payment provider settings changed: which fields, never the secret values.</summary>
    public const string PaymentIntegrationConfigured = "integration.payment_configured";

    /// <summary>The lab provider settings changed: which fields, never the key.</summary>
    public const string LabIntegrationConfigured = "integration.lab_configured";

    /// <summary>
    /// A staff member's calendar link was made. Anyone holding the link sees that person's
    /// appointments, so when it came into being is on record.
    /// </summary>
    public const string CalendarFeedCreated = "integration.calendar_feed_created";

    /// <summary>
    /// A webhook's URL, events, description or on/off switch changed (<c>docs/contract-extensions.md § 22</c>).
    /// Pointing events somewhere else is as weighty as creating the webhook, so the new
    /// destination is on record (without its query string).
    /// </summary>
    public const string WebhookUpdated = "integration.webhook_updated";

    /// <summary>A webhook was deleted: it stops, its secret is destroyed, its delivery log stays.</summary>
    public const string WebhookDeleted = "integration.webhook_deleted";

    /// <summary>A webhook got a new signing secret, shown once. Never the secret itself.</summary>
    public const string WebhookSecretRotated = "integration.webhook_secret_rotated";

    /// <summary>A failed webhook delivery was queued to be sent again.</summary>
    public const string WebhookRedelivered = "integration.webhook_redelivered";

    /// <summary>An API key was revoked before its expiry; from the next request it is refused.</summary>
    public const string ApiKeyRevoked = "integration.api_key_revoked";

    /// <summary>A staff member replaced their calendar link; the old link stopped working.</summary>
    public const string CalendarFeedRotated = "integration.calendar_feed_rotated";

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

    /// <summary>
    /// A pet was taken off the waiting list. The row is deleted, so this is the only trace that it
    /// was there (<c>docs/contract-extensions.md § 23</c>).
    /// </summary>
    public const string WaitingListEntryRemoved = "waiting_list.removed";

    public const string QueuePriorityChanged = "queue.priority_changed";

    /// <summary>
    /// A waiting-room screen was made and its setup code shown (<c>docs/contract-extensions.md § 30</c>):
    /// a credential that opens a branch's queue without a person signed in.
    /// </summary>
    public const string QueueDisplayCreated = "queue.display_created";

    /// <summary>The TV traded its setup code for its token. Nobody is signed in, so the row has no actor.</summary>
    public const string QueueDisplayClaimed = "queue.display_claimed";

    /// <summary>A screen's token was stopped. Its next request is refused.</summary>
    public const string QueueDisplayRevoked = "queue.display_revoked";

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
    /// A charge was added to a bill that already existed — medicine dispensed after its case closed,
    /// landing on the case's still-open invoice. The bill's total changed after it was issued.
    /// </summary>
    public const string InvoiceLineAdded = "invoice.line_added";

    // ---- Inventory -----------------------------------------------------------------------------

    /// <summary>A drug or supply entered the catalogue, with the price it will be charged at.</summary>
    public const string InventoryItemCreated = "inventory.item_created";

    /// <summary>
    /// "ปรับยอดหลังนับสต๊อก · ต้องระบุเหตุผล" — the books were changed to match a count. The one stock
    /// change with no document behind it, so who did it and why is the only account of it.
    /// </summary>
    public const string InventoryAdjusted = "inventory.adjusted";

    /// <summary>An order to a supplier was raised: money the clinic has committed to spend.</summary>
    public const string PurchaseOrderRaised = "inventory.purchase_order_raised";

    /// <summary>
    /// A catalogue item was renamed, repriced, relabelled, given a new reorder level, retired or
    /// brought back (<c>docs/contract-extensions.md § 25</c>). The price is what customers are
    /// charged, so the before and after are kept.
    /// </summary>
    public const string InventoryItemUpdated = "inventory.item_updated";

    /// <summary>
    /// What was still outstanding on a purchase order was called off (§ 25). Money the clinic had
    /// committed to spend, released.
    /// </summary>
    public const string PurchaseOrderCancelled = "inventory.purchase_order_cancelled";

    // ---- Loyalty, packages and promotions ------------------------------------------------------

    /// <summary>A package was sold to a customer, on a bill raised in the same step.</summary>
    public const string PackageSold = "package.sold";

    /// <summary>Uses of a package were spent on a bill line, which was charged at zero.</summary>
    public const string PackageUsed = "package.used";

    /// <summary>A package was cancelled because the bill it was sold on was voided or refunded.</summary>
    public const string PackageCancelled = "package.cancelled";

    /// <summary>A discount code was created. It is a standing approval to take money off bills.</summary>
    public const string CouponCreated = "coupon.created";

    /// <summary>A customer group was saved. It decides who campaigns reach.</summary>
    public const string SegmentCreated = "segment.created";

    /// <summary>A discount code was switched off before its last day (§ 25).</summary>
    public const string CouponDeactivated = "coupon.deactivated";

    /// <summary>
    /// Points were given or taken by hand, with a reason (§ 25). Points are money off future bills,
    /// and this is the one change to them that no bill accounts for.
    /// </summary>
    public const string LoyaltyAdjusted = "loyalty.adjusted";

    /// <summary>The points programme was changed (§ 25): rate, point value, levels or expiry.</summary>
    public const string LoyaltySettingsChanged = "settings.loyalty_changed";

    // ---- Articles on the public site (docs/contract-extensions.md § 26) -------------------------

    // Neither personal data nor who-can-do-what, so outside the two rules above on their face. They
    // are here because an article is the clinic speaking in public under a vet's name: when a page
    // says something it should not, "who put this up, and when" must have an answer.

    /// <summary>A draft article was written. Nobody outside the clinic sees it yet.</summary>
    public const string ArticleCreated = "article.created";

    /// <summary>An article's text, address or cover was changed. Lists the fields that changed.</summary>
    public const string ArticleUpdated = "article.updated";

    /// <summary>An article was put on the public site.</summary>
    public const string ArticlePublished = "article.published";

    /// <summary>An article was taken off the public site and is a draft again.</summary>
    public const string ArticleUnpublished = "article.unpublished";

    /// <summary>An article was deleted. The row stays so its address is never reused.</summary>
    public const string ArticleDeleted = "article.deleted";

    // ---- The clinic's privacy notice (docs/contract-extensions.md § 35) -------------------------

    // The notice is what every consent points at. "Who wrote this wording, who made it the clinic's
    // notice, and when" is the question a regulator asks first.

    /// <summary>A draft of the privacy notice was made, from the template or from another version.</summary>
    public const string PrivacyPolicyDraftCreated = "privacy_policy.draft_created";

    /// <summary>A draft of the privacy notice was rewritten. Counts and anchors, never the text.</summary>
    public const string PrivacyPolicyDraftUpdated = "privacy_policy.draft_updated";

    /// <summary>A version of the privacy notice was published and became the current one. It is fixed from now on.</summary>
    public const string PrivacyPolicyPublished = "privacy_policy.published";

    // ---- The staff AI assistant (docs/contract-extensions.md § 34) ------------------------------

    // One row per use, naming what was read. Never the prompt or the answer: those hold the
    // clinical record and the customer's words, and the audit log is not a second copy of either.

    /// <summary>
    /// A pet's record was summarised by the AI for a vet, freshly or from the cache. The model was
    /// shown the patient's file, so it is in the access log beside <see cref="PetHistoryViewed"/>.
    /// </summary>
    public const string AiPetSummary = "ai.pet_summary";

    /// <summary>A SOAP draft was made from a vet's short notes on an open case. Nothing was saved.</summary>
    public const string AiSoapDraft = "ai.soap_draft";

    /// <summary>A message to an owner was drafted: aftercare from a case, or a reply in a thread. Nothing was sent.</summary>
    public const string AiOwnerMessageDraft = "ai.owner_message_draft";

    /// <summary>The clinic's numbers were asked about. Details name the tools that ran, never the question.</summary>
    public const string AiAsk = "ai.ask";

    // ---- Reminders and reports -----------------------------------------------------------------

    /// <summary>
    /// A reminder rule was created. From then on it messages customers by itself in the clinic's
    /// name, so who wrote it and what it does are on record.
    /// </summary>
    public const string AutomationCreated = "automation.created";

    /// <summary>
    /// A reminder rule was edited or switched on or off (<c>docs/contract-extensions.md § 19</c>).
    /// Switching one on is what starts it messaging customers, so it is on record like creating one.
    /// </summary>
    public const string AutomationUpdated = "automation.updated";

    /// <summary>A reminder rule was deleted. Its history stays; who stopped it, and when, is here.</summary>
    public const string AutomationDeleted = "automation.deleted";

    /// <summary>
    /// A report was exported to a file. Money and personal data left the system in a form that can
    /// be forwarded, so who asked for it, of what, and for which branches is on record.
    /// </summary>
    public const string ReportExported = "report.exported";

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
        AiPetSummary,
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
        PetUpdated, PetDeceasedUndone,
        LabRequested, LabResultsRecorded, LabResultsReviewed, PrescriptionDispensed,
        CustomerCreated, CustomerMerged, CustomerLineLinked, CustomerLineUnlinked,
        ConsentRecorded, ConsentWithdrawn, DataRequestRaised, DataRequestClosed,
        NotificationBroadcast, LineIntegrationConfigured,
        WebhookCreated, ApiKeyCreated, PaymentIntegrationConfigured, LabIntegrationConfigured, CalendarFeedCreated,
        WebhookUpdated, WebhookDeleted, WebhookSecretRotated, WebhookRedelivered, ApiKeyRevoked, CalendarFeedRotated,
        SignedIn, SignInFailed, PasswordChanged, SessionRevoked, AccountRegistered, PasswordReset,
        PetAccessGranted, PetAccessRevoked, CustomerInvited,
        ClinicBootstrapped, CredentialsReset,
        StaffInvited, StaffInvitationAccepted, StaffUpdated, StaffDeactivated,
        StaffPublicProfileUpdated, StaffSeeded,
        RoleCreated, RolePermissionsReplaced, MedicalRecordSettingsChanged,
        AppointmentCancelled, AppointmentNoShow, CancellationPolicyChanged, WaitingListEntryRemoved, QueuePriorityChanged,
        QueueDisplayCreated, QueueDisplayClaimed, QueueDisplayRevoked,
        InvoiceIssued, InvoiceVoided, InvoiceDiscounted,
        PaymentCollected, PaymentRefunded, ReceiptIssued, ServicePricingChanged,
        ShiftsCreated, TimeOffRecorded, AssignmentRulesReplaced,
        AccountProfileUpdated, TwoFactorEnrolled, RecoveryCodeUsed, UserStatusChanged,
        AutomationCreated, AutomationUpdated, AutomationDeleted, ReportExported,
        InvoiceLineAdded, InventoryItemCreated, InventoryAdjusted, PurchaseOrderRaised,
        PackageSold, PackageUsed, PackageCancelled, CouponCreated, SegmentCreated,
        InventoryItemUpdated, PurchaseOrderCancelled, CouponDeactivated, LoyaltyAdjusted, LoyaltySettingsChanged,
        ArticleCreated, ArticleUpdated, ArticlePublished, ArticleUnpublished, ArticleDeleted,
        AiPetSummary, AiSoapDraft, AiOwnerMessageDraft, AiAsk,
        PrivacyPolicyDraftCreated, PrivacyPolicyDraftUpdated, PrivacyPolicyPublished,
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
    public const string WaitingList = "waiting_list";
    public const string QueueTicket = "queue_ticket";
    public const string QueueDisplay = "queue_display";
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
    public const string NotificationBatch = "notification_batch";
    public const string Integration = "integration";
    public const string Webhook = "webhook";
    public const string ApiKey = "api_key";
    public const string Automation = "automation";
    public const string ReportExport = "report_export";
    public const string InventoryItem = "inventory_item";
    public const string StockLot = "stock_lot";
    public const string PurchaseOrder = "purchase_order";
    public const string Package = "package";
    public const string Coupon = "coupon";
    public const string Segment = "segment";
    public const string Article = "article";
    public const string MessageThread = "message_thread";

    /// <summary>A version of the clinic's privacy notice (<c>docs/contract-extensions.md § 35</c>).</summary>
    public const string PrivacyPolicy = "privacy_policy";

    /// <summary>The staff AI assistant itself, for <c>ai.ask</c>, which reads reports rather than one record.</summary>
    public const string AiAssistant = "ai_assistant";
}
