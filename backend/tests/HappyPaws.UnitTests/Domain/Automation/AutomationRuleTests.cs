using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Automation;

/// <summary>What makes a reminder rule one the scheduler can run.</summary>
public class AutomationRuleTests
{
    private static AutomationRule Rule(
        AutomationTrigger trigger = AutomationTrigger.VaccineDue,
        int? days = 14,
        int? hours = null,
        TimeOnly? sendTime = null,
        string? vaccine = null,
        string? service = null,
        bool unconfirmedOnly = false) =>
        AutomationRule.Create(
            "เตือน", trigger, days, hours, sendTime, AutomationChannel.Line, "vaccine.due", enabled: false, "stf_1",
            vaccineName: vaccine, serviceId: service, unconfirmedOnly: unconfirmedOnly);

    [Fact]
    public void A_day_based_rule_sends_at_nine_unless_told_otherwise()
    {
        var rule = Rule();

        Assert.StartsWith("atm_", rule.Id);
        Assert.Equal(new TimeOnly(9, 0), rule.SendTime);
        Assert.False(rule.Enabled);
        Assert.Equal(AutomationSubjectKind.Vaccination, rule.SubjectKind);
    }

    [Fact]
    public void An_hour_based_rule_has_no_time_of_day()
    {
        var rule = Rule(AutomationTrigger.AppointmentUpcoming, days: null, hours: 24);

        Assert.Null(rule.SendTime);
        Assert.True(rule.Timing.IsHourBased);
        Assert.Equal(AutomationSubjectKind.Appointment, rule.SubjectKind);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(1, 2)]
    public void Exactly_one_offset_is_given(int? days, int? hours)
    {
        Assert.Throws<DomainException>(() => Rule(AutomationTrigger.AppointmentUpcoming, days, hours));
    }

    [Fact]
    public void A_vaccine_due_date_has_no_hours_to_count_back_from()
    {
        Assert.Throws<DomainException>(() => Rule(AutomationTrigger.VaccineDue, days: null, hours: 24));
    }

    [Theory]
    [InlineData(AutomationTrigger.VaccineDue, -31)]
    [InlineData(AutomationTrigger.VaccineDue, 91)]
    [InlineData(AutomationTrigger.AppointmentUpcoming, -1)]
    [InlineData(AutomationTrigger.TreatmentPlanSession, 31)]
    public void Day_offsets_stay_inside_their_range(AutomationTrigger trigger, int days)
    {
        Assert.Throws<DomainException>(() => Rule(trigger, days));
    }

    [Fact]
    public void An_overdue_follow_up_counts_days_after_the_due_date()
    {
        Assert.Equal(-7, Rule(AutomationTrigger.VaccineDue, days: -7).OffsetDays);
    }

    [Fact]
    public void Conditions_only_apply_to_the_trigger_they_mean_something_for()
    {
        Assert.Throws<DomainException>(() => Rule(AutomationTrigger.AppointmentUpcoming, days: 1, vaccine: "Rabies"));
        Assert.Throws<DomainException>(() => Rule(AutomationTrigger.VaccineDue, service: "svc_1"));
        Assert.Throws<DomainException>(() => Rule(AutomationTrigger.TreatmentPlanSession, days: 1, unconfirmedOnly: true));
    }

    [Fact]
    public void A_send_time_does_not_go_with_an_hour_offset()
    {
        Assert.Throws<DomainException>(() => Rule(AutomationTrigger.AppointmentUpcoming, days: null, hours: 2, sendTime: new TimeOnly(8, 0)));
    }

    [Fact]
    public void An_edit_replaces_timing_conditions_and_channel_but_never_what_the_rule_watches()
    {
        var rule = Rule(AutomationTrigger.AppointmentUpcoming, days: null, hours: 24);

        rule.Revise("เตือน 2 ชั่วโมง", null, 2, null, AutomationChannel.Sms, "brn_1", " สุนัข ", null, "svc_1", unconfirmedOnly: true);

        Assert.Equal(AutomationTrigger.AppointmentUpcoming, rule.Trigger);
        Assert.Equal(2, rule.OffsetHours);
        Assert.Null(rule.OffsetDays);
        Assert.Null(rule.SendTime);
        Assert.Equal(AutomationChannel.Sms, rule.Channel);
        Assert.Equal("brn_1", rule.BranchId);
        Assert.Equal("สุนัข", rule.Species);
        Assert.True(rule.UnconfirmedOnly);

        rule.Revise("เตือนเช้าวันนัด", 0, null, null, AutomationChannel.Auto, null, null, null, null, unconfirmedOnly: false);
        Assert.Equal(new TimeOnly(9, 0), rule.SendTime);
        Assert.Null(rule.BranchId);
    }

    [Fact]
    public void An_edit_is_held_to_the_rules_of_create()
    {
        var rule = Rule();

        Assert.Throws<DomainException>(() => rule.Revise("", 14, null, null, AutomationChannel.Line, null, null, null, null, false));
        Assert.Throws<DomainException>(() => rule.Revise("x", null, 24, null, AutomationChannel.Line, null, null, null, null, false));
        Assert.Throws<DomainException>(() => rule.Revise("x", 14, null, null, AutomationChannel.Line, null, null, null, "svc_1", false));
        Assert.Equal(14, rule.OffsetDays);
    }

    [Fact]
    public void A_deleted_rule_is_off_and_can_no_longer_be_changed()
    {
        var rule = Rule();
        rule.SetEnabled(true);
        var at = DateTimeOffset.UtcNow;

        rule.Delete("stf_2", at);

        Assert.True(rule.IsDeleted);
        Assert.False(rule.Enabled);
        Assert.Equal(at, rule.DeletedAt);
        Assert.Equal("stf_2", rule.DeletedByStaffId);
        Assert.Throws<DomainException>(() => rule.SetEnabled(true));
        Assert.Throws<DomainException>(() => rule.Delete("stf_2", at));
        Assert.Throws<DomainException>(() => rule.Revise("x", 14, null, null, AutomationChannel.Line, null, null, null, null, false));
    }
}
