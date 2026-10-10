using HappyPaws.Application.Automation;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Templates;
using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Notifications;

namespace HappyPaws.UnitTests.Application.Automation;

/// <summary>The template each trigger sends and the values it is filled with.</summary>
public class AutomationMessagesTests
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    private static AutomationRule Rule(AutomationTrigger trigger, AutomationChannel channel = AutomationChannel.Auto) =>
        AutomationRule.Create(
            "r",
            trigger,
            trigger == AutomationTrigger.AppointmentUpcoming ? null : 1,
            trigger == AutomationTrigger.AppointmentUpcoming ? 24 : null,
            null,
            channel,
            AutomationMessages.TemplateFor(trigger),
            enabled: true,
            "stf_1");

    [Theory]
    [InlineData(AutomationTrigger.VaccineDue, NotificationTemplates.VaccineDue)]
    [InlineData(AutomationTrigger.AppointmentUpcoming, NotificationTemplates.AppointmentReminder)]
    [InlineData(AutomationTrigger.TreatmentPlanSession, NotificationTemplates.TreatmentPlanSession)]
    [InlineData(AutomationTrigger.CustomerLapsed, NotificationTemplates.CustomerLapsed)]
    [InlineData(AutomationTrigger.VisitCompleted, NotificationTemplates.ReviewInvitation)]
    public void Each_trigger_sends_its_own_template_and_it_has_wording(AutomationTrigger trigger, string template)
    {
        Assert.Equal(template, AutomationMessages.TemplateFor(trigger));
        Assert.NotNull(NotificationTemplateCatalogue.Find(template));
    }

    [Fact]
    public void An_appointment_reminder_names_the_time_in_Bangkok_the_vet_and_the_appointment()
    {
        var subject = new AutomationSubject(
            AutomationSubjectKind.Appointment,
            "apt_1",
            new DateTimeOffset(2026, 10, 8, 3, 30, 0, TimeSpan.Zero),
            new DateOnly(2026, 10, 8),
            "cus_1",
            "คุณสมใจ",
            "pet_1",
            "ลูน่า",
            "brn_1",
            AppointmentId: "apt_1",
            StaffName: "หมอเอ");

        var request = AutomationMessages.RequestFor(Rule(AutomationTrigger.AppointmentUpcoming, AutomationChannel.Sms), subject, Bangkok);

        Assert.Equal("10:30", request.Data["time"]);
        Assert.Equal("หมอเอ", request.Data["vet_name"]);
        Assert.Equal("apt_1", request.Data["appointment_id"]);
        Assert.Contains("2569", request.Data["date"]);
        Assert.Equal("sms", request.Channel);
        Assert.Equal(DeliverySource.Automation, request.Source);
        Assert.Equal("brn_1", request.BranchId);
    }

    [Fact]
    public void A_vaccine_reminder_has_no_time_and_goes_on_any_channel()
    {
        var subject = new AutomationSubject(
            AutomationSubjectKind.Vaccination, "vac_1", null, new DateOnly(2026, 10, 20), "cus_1", "คุณสมใจ", "pet_1", "ลูน่า", null, Label: "Rabies");

        var request = AutomationMessages.RequestFor(Rule(AutomationTrigger.VaccineDue), subject, Bangkok);

        Assert.False(request.Data.ContainsKey("time"));
        Assert.Equal("Rabies", request.Data["vaccine_name"]);
        Assert.Null(request.Channel);
        Assert.Contains("Rabies", NotificationText.Render(request).Body);
    }

    [Fact]
    public void A_plan_session_reminder_carries_the_plan_and_the_session_number()
    {
        var subject = new AutomationSubject(
            AutomationSubjectKind.TreatmentPlanSession,
            "tpn_1:2",
            new DateTimeOffset(2026, 10, 9, 2, 0, 0, TimeSpan.Zero),
            new DateOnly(2026, 10, 9),
            "cus_1",
            "คุณสมใจ",
            "pet_1",
            "ลูน่า",
            null,
            Label: "คอร์สกายภาพ",
            SessionNumber: 2);

        var request = AutomationMessages.RequestFor(Rule(AutomationTrigger.TreatmentPlanSession), subject, Bangkok);

        Assert.Equal("2", request.Data["session_number"]);
        Assert.Equal("คอร์สกายภาพ", request.Data["plan_title"]);
        Assert.Equal("09:00", request.Data["time"]);
        Assert.Contains("ครั้งที่ 2", NotificationText.Render(request).Body);
    }
}
