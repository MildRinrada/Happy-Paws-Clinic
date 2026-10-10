using System.Globalization;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.Application.Automation;

/// <summary>
/// Which template each trigger sends, and the values it fills in.
/// <para>
/// One template per trigger. A rule cannot point a vaccine reminder at the appointment template,
/// because the values that template needs (the time, the vet) do not exist for a due date, and the
/// owner would get a sentence with holes in it.
/// </para>
/// </summary>
public static class AutomationMessages
{
    private static readonly CultureInfo Thai = CultureInfo.GetCultureInfo("th-TH");

    public static string TemplateFor(AutomationTrigger trigger) => trigger switch
    {
        AutomationTrigger.VaccineDue => NotificationTemplates.VaccineDue,
        AutomationTrigger.AppointmentUpcoming => NotificationTemplates.AppointmentReminder,
        AutomationTrigger.CustomerLapsed => NotificationTemplates.CustomerLapsed,
        _ => NotificationTemplates.TreatmentPlanSession,
    };

    /// <summary>The message a rule sends about one subject, ready for the dispatcher.</summary>
    public static NotificationRequest RequestFor(AutomationRule rule, AutomationSubject subject, TimeZoneInfo clinicZone) =>
        new(subject.CustomerId, rule.TemplateKey, DataFor(subject, clinicZone))
        {
            BranchId = subject.BranchId,
            Channel = rule.Channel == AutomationChannel.Auto ? null : AutomationWire.ToWire(rule.Channel),
            Source = DeliverySource.Automation,
        };

    private static Dictionary<string, string> DataFor(AutomationSubject subject, TimeZoneInfo clinicZone)
    {
        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["pet_name"] = subject.PetName,
            ["date"] = subject.Day.ToString("d MMM yyyy", Thai),
        };

        if (subject.StartsAt is { } startsAt)
        {
            data["time"] = TimeZoneInfo.ConvertTime(startsAt, clinicZone).ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        if (subject.AppointmentId is { } appointmentId)
        {
            // Lets the LINE message carry the confirm and cancel buttons of that appointment.
            data["appointment_id"] = appointmentId;
        }

        switch (subject.Kind)
        {
            case AutomationSubjectKind.Vaccination when subject.Label is { } vaccine:
                data["vaccine_name"] = vaccine;
                break;

            case AutomationSubjectKind.Appointment when subject.StaffName is { } vet:
                data["vet_name"] = vet;
                break;

            case AutomationSubjectKind.TreatmentPlanSession:
                data["plan_title"] = subject.Label ?? string.Empty;
                data["session_number"] = (subject.SessionNumber ?? 0).ToString(CultureInfo.InvariantCulture);
                break;
        }

        return data;
    }
}
