namespace HappyPaws.Domain.Integrations;

/// <summary>
/// The postback actions the contract supports on <c>POST /webhooks/line</c> — the button presses
/// a customer can send back from a notification message.
/// <para>
/// This endpoint is an <b>integration callback</b>. LINE is an outbound notification channel: it
/// authenticates nobody, issues no session, and a LINE address is delivery information on a
/// customer record, never a credential or a key.
/// </para>
/// <para>
/// A postback therefore arrives with no authenticated caller. The handler resolves the sending
/// address to the customer it is registered to — an address lookup, the way a mail system
/// attributes a reply — and then applies the same resource-level checks the portal endpoints use
/// before anything happens. The contract is explicit about that re-check
/// ("ทุกอันตรวจสิทธิ์ซ้ำฝั่งเซิร์ฟเวอร์ก่อนทำงานเสมอ").
/// </para>
/// </summary>
public static class LinePostbackActions
{
    public const string ConfirmAppointment = "confirm_appointment";
    public const string RescheduleAppointment = "reschedule_appointment";
    public const string CancelAppointment = "cancel_appointment";
    public const string ViewQueue = "view_queue";
    public const string OpenInvoice = "open_invoice";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ConfirmAppointment,
        RescheduleAppointment,
        CancelAppointment,
        ViewQueue,
        OpenInvoice,
    };
}
