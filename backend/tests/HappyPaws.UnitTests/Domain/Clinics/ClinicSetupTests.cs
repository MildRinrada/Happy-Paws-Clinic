using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Clinics;

public class OpeningHoursTests
{
    private static IEnumerable<DayHours> Week(TimeOnly opens, TimeOnly closes) =>
        Enum.GetValues<DayOfWeek>().Select(day => new DayHours(day, opens, closes));

    [Fact]
    public void A_clinic_that_never_set_its_hours_publishes_none()
    {
        // Not "nine to six". A default week would be opening hours on the clinic's website that
        // nobody at the clinic chose.
        var hours = OpeningHours.CreateDefault();

        Assert.False(hours.IsConfigured);
        Assert.Empty(hours.Days);
    }

    [Fact]
    public void A_week_is_stored_monday_first_whatever_order_it_arrived_in()
    {
        var hours = OpeningHours.CreateDefault();

        hours.ReplaceWeek(Week(new TimeOnly(9, 0), new TimeOnly(18, 0)).Reverse());

        Assert.Equal(DayOfWeek.Monday, hours.Days[0].Day);
        Assert.Equal(DayOfWeek.Sunday, hours.Days[^1].Day);
    }

    [Fact]
    public void A_week_missing_a_day_is_refused_rather_than_merged()
    {
        var hours = OpeningHours.CreateDefault();

        // Merging would keep whatever Sunday happened to be the last time anyone looked.
        Assert.Throws<DomainException>(() =>
            hours.ReplaceWeek(Week(new TimeOnly(9, 0), new TimeOnly(18, 0)).Take(6)));
    }

    [Fact]
    public void A_day_named_twice_is_refused()
    {
        var hours = OpeningHours.CreateDefault();
        var week = Week(new TimeOnly(9, 0), new TimeOnly(18, 0)).ToList();

        week[6] = week[0];

        Assert.Throws<DomainException>(() => hours.ReplaceWeek(week));
    }

    [Fact]
    public void A_day_that_closes_before_it_opens_is_refused()
    {
        var hours = OpeningHours.CreateDefault();

        Assert.Throws<DomainException>(() =>
            hours.ReplaceWeek(Week(new TimeOnly(18, 0), new TimeOnly(9, 0))));
    }

    [Fact]
    public void A_weekly_closed_day_has_no_times()
    {
        var hours = OpeningHours.CreateDefault();
        var week = Week(new TimeOnly(9, 0), new TimeOnly(18, 0))
            .Select(day => day.Day == DayOfWeek.Sunday ? DayHours.Closed(day.Day) : day);

        hours.ReplaceWeek(week);

        Assert.True(hours.Days.Single(day => day.Day == DayOfWeek.Sunday).IsClosed);
        Assert.False(hours.Days.Single(day => day.Day == DayOfWeek.Monday).IsClosed);
    }
}

public class QueueSettingsTests
{
    [Fact]
    public void The_defaults_print_the_contracts_own_example()
    {
        // The reference's worked example issues tkt_A014.
        Assert.Equal("A014", QueueSettings.CreateDefault().Format(14));
    }

    [Fact]
    public void A_clinic_may_choose_its_own_prefix_and_width()
    {
        var settings = QueueSettings.CreateDefault();

        settings.Update(numberPrefix: "q", numberDigits: 2);

        Assert.Equal("Q07", settings.Format(7));
    }

    [Fact]
    public void A_busy_day_grows_past_the_padding_instead_of_wrapping()
    {
        Assert.Equal("A1234", QueueSettings.CreateDefault().Format(1234));
    }

    [Fact]
    public void Bare_digits_are_allowed()
    {
        var settings = QueueSettings.CreateDefault();

        settings.Update(numberPrefix: string.Empty);

        Assert.Equal("014", settings.Format(14));
    }

    [Theory]
    [InlineData("ABCD")]
    [InlineData("A1")]
    [InlineData("ก")]
    public void A_prefix_that_cannot_be_read_aloud_or_printed_is_refused(string prefix)
    {
        Assert.Throws<DomainException>(() => QueueSettings.CreateDefault().Update(numberPrefix: prefix));
    }

    [Fact]
    public void A_field_that_was_not_sent_is_left_alone()
    {
        var settings = QueueSettings.CreateDefault();

        settings.Update(resetDaily: false);

        Assert.False(settings.ResetDaily);
        Assert.Equal("A", settings.NumberPrefix);
        Assert.Equal(3, settings.NumberDigits);
    }
}

public class ClinicProfileTests
{
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,AAAA")]
    [InlineData("/relative/logo.png")]
    public void A_logo_address_that_is_not_http_is_refused(string url)
    {
        // This value is rendered into an img on a public page. A stored javascript: or data:
        // address there is a stored XSS on the clinic's own website.
        Assert.Throws<DomainException>(() => ClinicProfile.CreateDefault().Update(logoUrl: url));
    }

    [Fact]
    public void An_empty_string_clears_a_field_and_an_absent_one_leaves_it()
    {
        var profile = ClinicProfile.CreateDefault();
        profile.Update(licenseNo: "สพ. 12/2567", operatorName: "น.สพ. สมชาย");

        profile.Update(licenseNo: string.Empty);

        Assert.Null(profile.LicenseNo);
        Assert.Equal("น.สพ. สมชาย", profile.OperatorName);
    }

    [Fact]
    public void A_clinic_cannot_be_left_without_a_name()
    {
        Assert.Throws<DomainException>(() => ClinicProfile.CreateDefault().Update(name: "  "));
    }

    [Fact]
    public void Half_a_coordinate_is_refused()
    {
        Assert.Throws<DomainException>(() => Branch.Create("b1", "สาขา", latitude: 13.73));
    }
}

public class MedicalRecordSettingsTests
{
    [Fact]
    public void A_clinic_with_no_examination_form_accepts_any_body_system()
    {
        Assert.True(MedicalRecordSettings.CreateDefault().AllowsExaminationSystem("anything at all"));
    }

    [Fact]
    public void A_configured_form_refuses_a_system_that_is_not_on_it()
    {
        var settings = MedicalRecordSettings.CreateDefault();

        settings.Update(examinationSystems: ["skin", "eyes"]);

        Assert.True(settings.AllowsExaminationSystem("Skin"));
        Assert.False(settings.AllowsExaminationSystem("cardiovascular"));
    }

    [Fact]
    public void A_required_field_the_system_cannot_check_is_refused()
    {
        // A setting that named a field nothing could verify would be a requirement in name only.
        Assert.Throws<DomainException>(() =>
            MedicalRecordSettings.CreateDefault().Update(requiredFields: ["mood"]));
    }

    [Fact]
    public void Nothing_clinical_is_visible_to_owners_until_the_clinic_says_so()
    {
        Assert.Empty(MedicalRecordSettings.CreateDefault().OwnerVisibleSections);
    }

    [Fact]
    public void An_empty_list_clears_a_setting_and_an_absent_one_leaves_it()
    {
        var settings = MedicalRecordSettings.CreateDefault();
        settings.Update(requiredFields: ["vitals"], ownerVisibleSections: ["diagnosis"]);

        settings.Update(requiredFields: []);

        Assert.Empty(settings.RequiredFields);
        Assert.Equal(["diagnosis"], settings.OwnerVisibleSections);
    }
}
