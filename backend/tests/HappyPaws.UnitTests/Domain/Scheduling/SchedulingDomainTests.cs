using HappyPaws.Application.Scheduling.Dtos;
using HappyPaws.Application.Scheduling.GetStaffAvailability;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.UnitTests.Domain.Scheduling;

public class ShiftRecurrenceTests
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    /// <summary>Monday 5 October 2026, 09:00 in Bangkok.</summary>
    private static readonly DateTimeOffset MondayNine = new(2026, 10, 5, 9, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public void Without_a_rule_there_is_one_shift() =>
        Assert.Equal([MondayNine], ShiftRecurrence.Expand(MondayNine, null, Bangkok));

    [Fact]
    public void A_weekly_rule_repeats_on_the_named_days_at_the_same_local_time()
    {
        var starts = ShiftRecurrence.Expand(MondayNine, "FREQ=WEEKLY;BYDAY=MO,WE,FR;UNTIL=20261016", Bangkok);

        Assert.Equal(
            [5, 7, 9, 12, 14, 16],
            starts.Select(start => TimeZoneInfo.ConvertTime(start, Bangkok).Day));
        Assert.All(starts, start => Assert.Equal(9, TimeZoneInfo.ConvertTime(start, Bangkok).Hour));
    }

    [Fact]
    public void Count_and_interval_are_honoured()
    {
        var starts = ShiftRecurrence.Expand(MondayNine, "RRULE:FREQ=DAILY;INTERVAL=2;COUNT=3", Bangkok);

        Assert.Equal([MondayNine, MondayNine.AddDays(2), MondayNine.AddDays(4)], starts);
    }

    [Fact]
    public void Days_before_the_first_shift_in_its_week_are_skipped()
    {
        var wednesday = MondayNine.AddDays(2);

        var starts = ShiftRecurrence.Expand(wednesday, "FREQ=WEEKLY;BYDAY=MO,WE;COUNT=2", Bangkok);

        Assert.Equal([wednesday, MondayNine.AddDays(7)], starts);
    }

    [Theory]
    [InlineData("FREQ=WEEKLY;BYDAY=MO")]
    [InlineData("FREQ=MONTHLY;COUNT=3")]
    [InlineData("FREQ=WEEKLY;COUNT=2;UNTIL=20261231")]
    [InlineData("FREQ=DAILY;BYDAY=MO;COUNT=2")]
    [InlineData("FREQ=WEEKLY;BYDAY=XX;COUNT=2")]
    [InlineData("FREQ=WEEKLY;BYSETPOS=1;COUNT=2")]
    [InlineData("FREQ=DAILY;COUNT=201")]
    [InlineData("FREQ=DAILY;UNTIL=20280101")]
    public void Rules_it_cannot_follow_exactly_are_refused(string rule)
    {
        var refused = Assert.Throws<DomainException>(() => ShiftRecurrence.Expand(MondayNine, rule, Bangkok));

        Assert.Equal("invalid_recurrence", refused.Code);
    }
}

public class StaffShiftCarveOutTests
{
    private static readonly DateTimeOffset Nine = new(2026, 10, 5, 9, 0, 0, TimeSpan.FromHours(7));
    private static readonly DateTimeOffset Now = Nine.AddDays(-1);

    private static StaffShift Shift() => StaffShift.Create("stf_1", "brn_1", Nine, Nine.AddHours(9));

    [Fact]
    public void Leave_in_the_middle_leaves_a_shift_either_side_and_cancels_the_original()
    {
        var shift = Shift();

        var pieces = shift.CarveOut(Nine.AddHours(3), Nine.AddHours(4), Now);

        Assert.False(shift.IsActive);
        Assert.Equal(
            [(Nine, Nine.AddHours(3)), (Nine.AddHours(4), Nine.AddHours(9))],
            pieces.Select(piece => (piece.StartsAt, piece.EndsAt)));
        Assert.All(pieces, piece => Assert.Equal("brn_1", piece.BranchId));
    }

    [Fact]
    public void Leave_over_the_whole_shift_leaves_nothing() =>
        Assert.Empty(Shift().CarveOut(Nine.AddHours(-1), Nine.AddHours(10), Now));

    [Fact]
    public void Leave_that_does_not_touch_the_shift_changes_nothing()
    {
        var shift = Shift();

        Assert.Empty(shift.CarveOut(Nine.AddHours(9), Nine.AddHours(12), Now));
        Assert.True(shift.IsActive);
    }
}

public class StaffTimeOffTests
{
    private static readonly DateTimeOffset Nine = new(2026, 10, 5, 9, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public void Leave_must_end_after_it_starts() =>
        Assert.Throws<DomainException>(() => StaffTimeOff.Record("stf_1", Nine, Nine, null, null));

    [Fact]
    public void Leave_longer_than_ninety_days_is_refused() =>
        Assert.Throws<DomainException>(() => StaffTimeOff.Record("stf_1", Nine, Nine.AddDays(91), null, null));

    [Fact]
    public void A_blank_reason_is_stored_as_none() =>
        Assert.Null(StaffTimeOff.Record("stf_1", Nine, Nine.AddHours(4), "   ", null).Reason);
}

public class AssignmentRulesTests
{
    private static AssignmentRules With(params AssignmentRule[] rules)
    {
        var set = AssignmentRules.CreateDefault();
        set.Replace(rules);
        return set;
    }

    [Fact]
    public void The_people_a_matching_rule_names_come_first_in_its_order()
    {
        var rules = With(new AssignmentRule("cat", null, ["stf_c", "stf_b"]));

        Assert.Equal(["stf_c", "stf_b", "stf_a"], rules.Rank(["stf_a", "stf_b", "stf_c"], "svc_1", "Cat"));
    }

    [Fact]
    public void Without_a_match_the_order_is_stable() =>
        Assert.Equal(
            ["stf_a", "stf_b"],
            With(new AssignmentRule("cat", null, ["stf_b"])).Rank(["stf_b", "stf_a"], "svc_1", "สุนัข"));

    [Fact]
    public void The_first_matching_rule_wins()
    {
        var rules = With(
            new AssignmentRule("cat", "svc_dental", ["stf_dentist"]),
            new AssignmentRule("cat", null, ["stf_cat"]));

        Assert.Equal("stf_dentist", rules.Rank(["stf_cat", "stf_dentist"], "svc_dental", "cat")[0]);
        Assert.Equal("stf_cat", rules.Rank(["stf_cat", "stf_dentist"], "svc_checkup", "cat")[0]);
    }

    [Fact]
    public void A_preferred_person_who_is_not_free_is_simply_skipped() =>
        Assert.Equal(["stf_a"], With(new AssignmentRule(null, "svc_1", ["stf_busy"])).Rank(["stf_a"], "svc_1", null));

    [Fact]
    public void A_rule_must_say_which_cases_it_is_for() =>
        Assert.Throws<DomainException>(() => With(new AssignmentRule(" ", null, ["stf_a"])));

    [Fact]
    public void A_rule_must_name_someone() =>
        Assert.Throws<DomainException>(() => With(new AssignmentRule("cat", null, [])));
}

public class FreeTimeTests
{
    private static readonly DateTimeOffset Nine = new(2026, 10, 5, 9, 0, 0, TimeSpan.FromHours(7));

    private static TimeWindowDto Window(int fromHour, int toHour) => new(Nine.AddHours(fromHour - 9), Nine.AddHours(toHour - 9));

    [Fact]
    public void Booked_time_is_taken_out_of_the_shift()
    {
        var free = GetStaffAvailabilityHandler.Subtract([Window(9, 17)], [Window(10, 11), Window(10, 12), Window(15, 18)]);

        Assert.Equal([Window(9, 10), Window(12, 15)], free);
    }

    [Fact]
    public void A_shift_with_nothing_booked_is_all_free() =>
        Assert.Equal([Window(9, 12), Window(13, 17)], GetStaffAvailabilityHandler.Subtract([Window(13, 17), Window(9, 12)], []));
}
