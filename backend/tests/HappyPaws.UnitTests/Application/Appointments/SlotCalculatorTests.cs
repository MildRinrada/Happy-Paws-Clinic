using HappyPaws.Application.Appointments.Availability;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Scheduling;

namespace HappyPaws.UnitTests.Application.Appointments;

/// <summary>
/// Slot arithmetic — the part of booking most likely to be quietly wrong.
/// <para>
/// All pure, no database. Boundary behaviour (a slot that ends exactly when the shift does,
/// appointments that touch without overlapping, a hold that has just lapsed) is cheap to pin down
/// here and expensive to discover through HTTP.
/// </para>
/// </summary>
public class SlotCalculatorTests
{
    private static readonly DateTimeOffset Nine = new(2026, 8, 21, 9, 0, 0, TimeSpan.Zero);

    private static StaffShift Shift(string staffId, DateTimeOffset from, DateTimeOffset to) =>
        StaffShift.Create(staffId, "brn_1", from, to);

    private static Appointment At(string staffId, DateTimeOffset from, int minutes) =>
        Appointment.Book("brn_1", "cus_1", "pet_1", "svc_1", staffId, from, from.AddMinutes(minutes));

    private static SlotQuery Query(
        IReadOnlyList<StaffShift> shifts,
        IReadOnlyList<Appointment>? occupied = null,
        IReadOnlyList<SlotHold>? holds = null,
        int slotMinutes = 30,
        int durationMinutes = 30,
        IReadOnlyCollection<string>? eligible = null) =>
        new(
            shifts,
            occupied ?? [],
            holds ?? [],
            slotMinutes,
            durationMinutes,
            EarliestStart: Nine.AddYears(-1),
            LatestStart: Nine.AddYears(1),
            EligibleStaffIds: eligible ?? []);

    [Fact]
    public void A_three_hour_shift_on_a_thirty_minute_grid_yields_six_slots()
    {
        var slots = SlotCalculator.Enumerate(Query([Shift("stf_1", Nine, Nine.AddHours(3))]));

        Assert.Equal(6, slots.Count);
        Assert.Equal(Nine, slots[0].StartsAt);
        Assert.Equal(Nine.AddHours(2).AddMinutes(30), slots[^1].StartsAt);
    }

    [Fact]
    public void A_slot_that_would_run_past_the_end_of_the_shift_is_not_offered()
    {
        // 09:00–10:00 with a 45-minute service fits 09:00 and nothing else: 09:30 would end at
        // 10:15, a quarter of an hour after the vet has gone home.
        var slots = SlotCalculator.Enumerate(
            Query([Shift("stf_1", Nine, Nine.AddHours(1))], durationMinutes: 45));

        Assert.Single(slots);
        Assert.Equal(Nine, slots[0].StartsAt);
    }

    [Fact]
    public void The_grid_is_anchored_at_the_shift_start_not_at_the_hour()
    {
        // A vet starting at 09:10 offers 09:10, not 09:30 — otherwise the first twenty minutes of
        // every irregular shift are lost to a clock-aligned grid.
        var start = Nine.AddMinutes(10);
        var slots = SlotCalculator.Enumerate(Query([Shift("stf_1", start, start.AddHours(1))]));

        Assert.Equal(start, slots[0].StartsAt);
        Assert.Equal(start.AddMinutes(30), slots[1].StartsAt);
    }

    [Fact]
    public void An_existing_appointment_removes_exactly_the_slots_it_covers()
    {
        var slots = SlotCalculator.Enumerate(Query(
            [Shift("stf_1", Nine, Nine.AddHours(2))],
            occupied: [At("stf_1", Nine.AddMinutes(30), 30)]));

        Assert.Equal(3, slots.Count);
        Assert.DoesNotContain(slots, slot => slot.StartsAt == Nine.AddMinutes(30));
    }

    [Fact]
    public void Back_to_back_appointments_do_not_collide()
    {
        // Half-open windows: a visit ending at 09:30 and one starting at 09:30 are not a conflict.
        // Treating touching windows as overlapping would halve every clinic's capacity.
        var free = SlotCalculator.IsFree(
            [At("stf_1", Nine, 30)],
            [],
            "stf_1",
            Nine.AddMinutes(30),
            Nine.AddHours(1));

        Assert.True(free);
    }

    [Fact]
    public void A_cancelled_appointment_stops_blocking_its_slot()
    {
        var cancelled = At("stf_1", Nine.AddMinutes(30), 30);
        cancelled.Cancel(Nine, "usr_1", "เลื่อนไปวันอื่น");

        var slots = SlotCalculator.Enumerate(Query(
            [Shift("stf_1", Nine, Nine.AddHours(2))], occupied: [cancelled]));

        Assert.Equal(4, slots.Count);
    }

    [Fact]
    public void Another_vets_appointment_does_not_block_this_vet()
    {
        var slots = SlotCalculator.Enumerate(Query(
            [Shift("stf_1", Nine, Nine.AddHours(1))],
            occupied: [At("stf_2", Nine, 30)]));

        Assert.Equal(2, slots.Count);
    }

    [Fact]
    public void A_live_hold_blocks_and_a_lapsed_one_does_not()
    {
        var hold = SlotHold.Create("brn_1", "svc_1", "stf_1", "cus_9", Nine, Nine.AddMinutes(30), Nine);

        Assert.False(SlotCalculator.IsFree([], [hold], "stf_1", Nine, Nine.AddMinutes(30)));

        // The repository is what filters lapsed holds out; once it does, the calculator sees a
        // free slot. Consuming the hold has the same effect for a caller redeeming their own.
        hold.Consume("cus_9", Nine.AddMinutes(1));
        Assert.False(hold.IsHoldingAt(Nine.AddMinutes(2)));
    }

    [Fact]
    public void Only_staff_eligible_for_the_service_are_offered()
    {
        var slots = SlotCalculator.Enumerate(Query(
            [Shift("stf_1", Nine, Nine.AddHours(1)), Shift("stf_2", Nine, Nine.AddHours(1))],
            eligible: ["stf_2"]));

        Assert.All(slots, slot => Assert.Equal("stf_2", slot.StaffId));
    }

    [Fact]
    public void An_empty_eligibility_list_means_any_vet_on_shift()
    {
        var slots = SlotCalculator.Enumerate(Query(
            [Shift("stf_1", Nine, Nine.AddHours(1)), Shift("stf_2", Nine, Nine.AddHours(1))]));

        Assert.Equal(4, slots.Count);
    }

    [Fact]
    public void Slots_before_the_lead_time_are_withheld()
    {
        var query = Query([Shift("stf_1", Nine, Nine.AddHours(2))]) with
        {
            EarliestStart = Nine.AddHours(1),
        };

        var slots = SlotCalculator.Enumerate(query);

        Assert.All(slots, slot => Assert.True(slot.StartsAt >= Nine.AddHours(1)));
        Assert.Equal(2, slots.Count);
    }

    [Fact]
    public void A_cancelled_shift_offers_nothing()
    {
        var shift = Shift("stf_1", Nine, Nine.AddHours(3));
        shift.Cancel(Nine);

        Assert.Empty(SlotCalculator.Enumerate(Query([shift])));
    }

    [Fact]
    public void Shift_coverage_requires_the_whole_window_to_fit()
    {
        var shifts = new[] { Shift("stf_1", Nine, Nine.AddHours(1)) };

        Assert.True(SlotCalculator.IsCoveredByShift(shifts, "stf_1", Nine, Nine.AddMinutes(30)));
        // Ends fifteen minutes after the shift does.
        Assert.False(SlotCalculator.IsCoveredByShift(shifts, "stf_1", Nine.AddMinutes(45), Nine.AddMinutes(75)));
        Assert.False(SlotCalculator.IsCoveredByShift(shifts, "stf_2", Nine, Nine.AddMinutes(30)));
    }

    [Fact]
    public void Suggestions_are_distinct_clinic_local_times_capped_at_three()
    {
        // Two vets both free at the same times: the customer wants three times offered, not the
        // same time three times over.
        var slots = SlotCalculator.Enumerate(Query(
            [Shift("stf_1", Nine, Nine.AddHours(3)), Shift("stf_2", Nine, Nine.AddHours(3))]));

        var bangkok = TimeZoneInfo.CreateCustomTimeZone("clinic", TimeSpan.FromHours(7), "clinic", "clinic");
        var suggested = SlotCalculator.SuggestTimes(slots, bangkok);

        Assert.Equal(["16:00", "16:30", "17:00"], suggested);
    }

    [Fact]
    public void Slots_are_ordered_by_time_then_by_vet()
    {
        var slots = SlotCalculator.Enumerate(Query(
            [Shift("stf_2", Nine, Nine.AddHours(1)), Shift("stf_1", Nine, Nine.AddHours(1))]));

        Assert.Equal([Nine, Nine, Nine.AddMinutes(30), Nine.AddMinutes(30)], slots.Select(s => s.StartsAt));
        Assert.Equal(["stf_1", "stf_2", "stf_1", "stf_2"], slots.Select(s => s.StaffId));
    }
}
