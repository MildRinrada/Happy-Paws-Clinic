using System.Reflection;
using HappyPaws.Application.Notifications;
using HappyPaws.Application.Notifications.Templates;

namespace HappyPaws.UnitTests.Application.Notifications;

/// <summary>
/// The wording of every message, and the two rules that would fail quietly: a template whose text
/// reads a value nobody declared, and a short SMS that grew into two paid parts.
/// </summary>
public class NotificationTemplateTests
{
    /// <summary>A clinic name of twenty characters, the length the existing OTP text was sized for.</summary>
    private const string Clinic = "คลินิกสัตว์แฮปปี้พอว";

    private static readonly IReadOnlyDictionary<string, string> Sample = new Dictionary<string, string>
    {
        ["clinic"] = Clinic,
        ["portal_url"] = "https://clinic.example/portal",
        ["pet_name"] = "ลูน่า",
        ["vet_name"] = "หมอเอ",
        ["date"] = "12 ต.ค.",
        ["time"] = "10:30",
        ["vaccine_name"] = "พิษสุนัขบ้า",
        ["number"] = "A014",
        ["room"] = "ห้อง 2",
        ["receipt_number"] = "RC2610-00012",
        ["amount"] = "1250.00",
        ["service_name"] = "ตรวจสุขภาพ",
        ["plan_title"] = "คอร์สกายภาพบำบัด",
        ["session_number"] = "12",
    };

    [Fact]
    public void A_value_is_filled_and_an_optional_segment_needs_all_its_values()
    {
        Assert.Equal("เชิญที่ห้องตรวจ 2", TemplateText.Fill("เชิญที่ห้องตรวจ[ {room}]", new Dictionary<string, string> { ["room"] = "2" }));
        Assert.Equal("เชิญที่ห้องตรวจ", TemplateText.Fill("เชิญที่ห้องตรวจ[ {room}]", new Dictionary<string, string>()));
        Assert.Equal("เชิญที่ห้องตรวจ", TemplateText.Fill("เชิญที่ห้องตรวจ[ {room}]", new Dictionary<string, string> { ["room"] = "  " }));
    }

    [Fact]
    public void A_value_is_inserted_as_typed_and_never_read_as_template()
    {
        var text = TemplateText.Fill("{message}", new Dictionary<string, string> { ["message"] = "ราคา [พิเศษ] {pet_name}" });

        Assert.Equal("ราคา [พิเศษ] {pet_name}", text);
    }

    [Fact]
    public void Every_template_key_in_use_has_wording()
    {
        var keys = typeof(NotificationTemplates)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(constant => constant.IsLiteral && constant.FieldType == typeof(string))
            .Select(constant => (string)constant.GetRawConstantValue()!);

        Assert.All(keys, key => Assert.NotNull(NotificationTemplateCatalogue.Find(key)));
    }

    [Fact]
    public void A_template_reads_only_the_values_it_declares()
    {
        // A placeholder nobody declared is one a staff send can never fill, so it would always be
        // blank. clinic and portal_url are supplied by the sender itself.
        foreach (var template in NotificationTemplateCatalogue.All)
        {
            var declared = template.Variables.Select(variable => variable.Name).Concat(["clinic", "portal_url"]).ToHashSet();
            var used = TemplateText.NamesIn(template.Title + template.Line + template.Sms);

            Assert.True(used.IsSubsetOf(declared), $"{template.Key} reads {string.Join(", ", used.Except(declared))}");
        }
    }

    [Theory]
    [InlineData(NotificationTemplates.AppointmentReminder)]
    [InlineData(NotificationTemplates.VaccineDue)]
    [InlineData(NotificationTemplates.QueueCalled)]
    [InlineData(NotificationTemplates.ReceiptIssued)]
    [InlineData(NotificationTemplates.ConsultationSummary)]
    [InlineData(NotificationTemplates.PetDeceasedAppointmentsCancelled)]
    [InlineData(NotificationTemplates.TreatmentPlanSession)]
    public void A_short_notice_fits_one_Thai_SMS_part(string key)
    {
        var sms = TemplateText.Fill(NotificationTemplateCatalogue.Find(key)!.Sms, Sample);

        Assert.True(sms.Length <= 70, $"{key}: {sms.Length} characters is more than one Thai SMS part ({sms})");
    }

    [Fact]
    public void The_visit_summary_does_not_carry_the_diagnosis_off_the_clinics_own_system()
    {
        var template = NotificationTemplateCatalogue.Find(NotificationTemplates.ConsultationSummary)!;

        Assert.DoesNotContain("{diagnosis}", template.Line + template.Sms + template.Title);
    }

    [Fact]
    public void Only_messages_that_name_no_real_record_can_be_sent_by_hand()
    {
        var sendable = NotificationTemplateCatalogue.All.Where(template => template.StaffSendable).Select(template => template.Key);

        string[] expected = [NotificationTemplates.AppointmentReminder, NotificationTemplates.ClinicMessage, NotificationTemplates.VaccineDue];

        Assert.Equal(expected.Order(StringComparer.Ordinal), sendable.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void The_new_templates_read_in_the_inbox_the_same_as_on_LINE()
    {
        var inbox = NotificationText.Render(new NotificationRequest(
            "cus_1",
            NotificationTemplates.AppointmentReminder,
            new Dictionary<string, string> { ["pet_name"] = "ลูน่า", ["date"] = "12 ต.ค.", ["time"] = "10:30" }));

        Assert.Equal("เตือนนัดหมาย", inbox.Title);
        Assert.Contains("ลูน่า", inbox.Body);
        Assert.DoesNotContain("{", inbox.Body);
        Assert.DoesNotContain("กับ", inbox.Body);
    }
}
