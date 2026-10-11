using HappyPaws.Domain.Common;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.UnitTests.Domain.Scheduling;

/// <summary>Moving one block, and telling history from what is still to come (docs/contract-extensions.md § 47).</summary>
public class ShiftChangeTests
{
    private static readonly DateTimeOffset Nine = new(2026, 10, 12, 9, 0, 0, TimeSpan.FromHours(7));

    private static StaffShift NineToSix() => StaffShift.Create("stf_a", "brn_a", Nine, Nine.AddHours(9));

    [Fact]
    public void Rescheduling_moves_the_block_and_keeps_its_id()
    {
        var shift = NineToSix();
        var id = shift.Id;

        shift.Reschedule(Nine.AddHours(1), Nine.AddHours(8));

        Assert.Equal(id, shift.Id);
        Assert.Equal(Nine.AddHours(1), shift.StartsAt);
        Assert.Equal(Nine.AddHours(8), shift.EndsAt);
    }

    [Fact]
    public void An_end_before_the_start_is_refused()
    {
        var shift = NineToSix();

        Assert.Throws<DomainException>(() => shift.Reschedule(Nine.AddHours(3), Nine.AddHours(3)));
        Assert.Equal(Nine, shift.StartsAt);
    }

    [Fact]
    public void A_cancelled_block_cannot_be_moved()
    {
        var shift = NineToSix();
        shift.Cancel(Nine.AddDays(-1));

        Assert.Throws<DomainException>(() => shift.Reschedule(Nine, Nine.AddHours(4)));
    }

    [Fact]
    public void Started_and_ended_are_read_against_now()
    {
        var shift = NineToSix();

        Assert.False(shift.HasStarted(Nine.AddMinutes(-1)));
        Assert.True(shift.HasStarted(Nine));
        Assert.False(shift.HasEnded(Nine.AddHours(8)));
        Assert.True(shift.HasEnded(Nine.AddHours(9)));
    }
}
