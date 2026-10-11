using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Scheduling;
using HappyPaws.Application.Scheduling.ChangeShift;
using HappyPaws.Application.Scheduling.CopyWeek;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.UnitTests.Application.Scheduling;

/// <summary>
/// The rules behind changing, removing and copying shifts that need no database
/// (docs/contract-extensions.md § 47 and § 49).
/// </summary>
public class ShiftEditRulesTests
{
    private static readonly DateTimeOffset Nine = new(2026, 10, 12, 9, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public void Shortening_both_ends_uncovers_the_time_either_side()
    {
        var parts = ChangeShiftHandler.Uncovered(Nine, Nine.AddHours(9), Nine.AddHours(1), Nine.AddHours(8));

        Assert.Equal([(Nine, Nine.AddHours(1)), (Nine.AddHours(8), Nine.AddHours(9))], parts);
    }

    [Fact]
    public void Lengthening_uncovers_nothing()
    {
        Assert.Empty(ChangeShiftHandler.Uncovered(Nine, Nine.AddHours(4), Nine.AddHours(-1), Nine.AddHours(5)));
    }

    [Fact]
    public void Moving_to_another_time_altogether_uncovers_the_whole_old_block()
    {
        var parts = ChangeShiftHandler.Uncovered(Nine, Nine.AddHours(4), Nine.AddHours(6), Nine.AddHours(10));

        Assert.Equal([(Nine, Nine.AddHours(4))], parts);
    }

    [Fact]
    public void An_overlap_with_someone_else_is_not_a_clash()
    {
        var theirs = StaffShift.Create("stf_b", "brn_a", Nine, Nine.AddHours(8));

        ShiftClashes.EnsureNoOverlap([new PlannedShift("stf_a", Nine, Nine.AddHours(8))], [theirs]);
    }

    [Fact]
    public void An_overlap_with_the_same_person_names_them_in_the_clash()
    {
        var mine = StaffShift.Create("stf_a", "brn_b", Nine.AddHours(2), Nine.AddHours(4));

        var refused = Assert.Throws<ConflictException>(() =>
            ShiftClashes.EnsureNoOverlap([new PlannedShift("stf_a", Nine, Nine.AddHours(8))], [mine]));

        Assert.Equal(ErrorCodes.ShiftOverlap, refused.Code);
        var clash = Assert.Single((IEnumerable<Dictionary<string, object?>>)refused.Details!["conflicts"]);
        Assert.Equal("stf_a", clash["staff_id"]);
        Assert.Equal(mine.Id, clash["shift_id"]);
    }

    [Fact]
    public void Time_off_of_the_same_person_is_a_clash()
    {
        var leave = StaffTimeOff.Record("stf_a", Nine.AddHours(4), Nine.AddHours(12), "ลาป่วย", null);

        var refused = Assert.Throws<ConflictException>(() =>
            ShiftClashes.EnsureNotOnTimeOff([new PlannedShift("stf_a", Nine, Nine.AddHours(8))], [leave]));

        Assert.Equal(ErrorCodes.StaffOnTimeOff, refused.Code);
    }

    [Fact]
    public void Both_times_are_needed_to_change_a_shift()
    {
        var result = new ChangeShiftValidator().Validate(new ChangeShiftCommand("shf_a", Nine, null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangeShiftCommand.EndsAt));
    }

    [Fact]
    public void A_changed_shift_follows_the_same_length_rule_as_a_new_one()
    {
        var validator = new ChangeShiftValidator();

        Assert.False(validator.Validate(new ChangeShiftCommand("shf_a", Nine, Nine.AddHours(25))).IsValid);
        Assert.False(validator.Validate(new ChangeShiftCommand("shf_a", Nine, Nine)).IsValid);
        Assert.True(validator.Validate(new ChangeShiftCommand("shf_a", Nine, Nine.AddHours(9))).IsValid);
    }

    [Theory]
    [InlineData("2026-10-05", "2026-10-12", true)]
    [InlineData("2026-10-06", "2026-10-12", false)]
    [InlineData("2026-10-05", "2026-10-14", false)]
    [InlineData("2026-10-05", "2026-10-05", false)]
    public void Weeks_are_named_by_two_different_mondays(string from, string to, bool valid)
    {
        var result = new CopyWeekValidator().Validate(
            new CopyWeekCommand("brn_a", DateOnly.Parse(from), DateOnly.Parse(to)));

        Assert.Equal(valid, result.IsValid);
    }
}
