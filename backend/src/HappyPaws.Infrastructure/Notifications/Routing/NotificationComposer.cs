using HappyPaws.Application.Integrations;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Templates;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Messaging;
using HappyPaws.Infrastructure.Notifications.Line;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Notifications.Routing;

/// <summary>
/// Turns a request into words for every channel at once: the catalogue's wording filled with the
/// request's values, the clinic's name and the portal address, plus the chat buttons that fit.
/// <para>
/// A template the catalogue does not know is still sent, in the inbox's wording, rather than
/// dropped: a module that adds a message before adding its wording should produce a plain message,
/// not silence.
/// </para>
/// </summary>
internal sealed class NotificationComposer(ClinicNameReader clinic, IOptions<LineOptions> line)
{
    public async Task<ComposedMessage> ComposeAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>(request.Data, StringComparer.Ordinal)
        {
            ["clinic"] = await clinic.ReadAsync(cancellationToken),
            ["portal_url"] = line.Value.PortalUrl,
        };

        if (NotificationTemplateCatalogue.Find(request.TemplateKey) is { } template)
        {
            return new ComposedMessage(
                TemplateText.Fill(template.Title, values),
                TemplateText.Fill(template.Line, values),
                TemplateText.Fill(template.Sms, values),
                Buttons(request.Data));
        }

        var inbox = NotificationText.Render(request);

        return new ComposedMessage(inbox.Title, $"{inbox.Title} {inbox.Body}", $"{values["clinic"]}: {inbox.Title}", Buttons(request.Data));
    }

    /// <summary>
    /// The contract's chat buttons, offered where the message names the record they act on. Every
    /// one is checked again against the customer when it comes back (<c>LinePostbackExecutor</c>).
    /// </summary>
    private static List<LineQuickReply> Buttons(IReadOnlyDictionary<string, string> data)
    {
        var buttons = new List<LineQuickReply>();

        if (data.TryGetValue(LinePostbackData.AppointmentId, out var appointmentId) && appointmentId.Length > 0)
        {
            buttons.Add(new(LineButtons.Confirm, LinePostbackData.Build(LinePostbackActions.ConfirmAppointment, (LinePostbackData.AppointmentId, appointmentId))));
            buttons.Add(new(LineButtons.Reschedule, LinePostbackData.Build(LinePostbackActions.RescheduleAppointment, (LinePostbackData.AppointmentId, appointmentId))));
            buttons.Add(new(LineButtons.Cancel, LinePostbackData.Build(LinePostbackActions.CancelAppointment, (LinePostbackData.AppointmentId, appointmentId))));
        }

        if (data.TryGetValue(LinePostbackData.TicketId, out var ticketId) && ticketId.Length > 0)
        {
            buttons.Add(new(LineButtons.ViewQueue, LinePostbackData.Build(LinePostbackActions.ViewQueue, (LinePostbackData.TicketId, ticketId))));
        }

        if (data.TryGetValue(LinePostbackData.InvoiceId, out var invoiceId) && invoiceId.Length > 0)
        {
            buttons.Add(new(LineButtons.OpenInvoice, LinePostbackData.Build(LinePostbackActions.OpenInvoice, (LinePostbackData.InvoiceId, invoiceId))));
        }

        return buttons;
    }
}

/// <summary>The chat buttons' labels. LINE cuts a label at 20 characters.</summary>
internal static class LineButtons
{
    public const string Confirm = "ยืนยันนัด";
    public const string Reschedule = "เลื่อนนัด";
    public const string Cancel = "ยกเลิกนัด";
    public const string ViewQueue = "ดูคิวของฉัน";
    public const string OpenInvoice = "ดูบิล";
}
