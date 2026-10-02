using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Appointments;

/// <summary>
/// The appointment state machine, including the edges that must <b>not</b> exist.
/// <para>
/// A transition table is only as good as the transitions it refuses. Asserting that a cancelled
/// appointment cannot be checked in is worth more than asserting that a booked one can, because
/// the second is exercised by every happy-path test in the suite and the first is exercised by
/// nothing until a customer taps a stale reminder link.
/// </para>
/// </summary>
public class AppointmentStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 3, 0, 0, TimeSpan.Zero);

    private static Appointment Booked() => Appointment.Book(
        "brn_1", "cus_1", "pet_1", "svc_1", "stf_1", Now.AddHours(4), Now.AddHours(4).AddMinutes(30));

    [Fact]
    public void A_new_booking_starts_booked_and_holds_its_slot()
    {
        var appointment = Booked();

        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
        Assert.True(appointment.OccupiesSlot);
    }

    [Fact]
    public void Booking_with_an_end_before_its_start_is_refused()
    {
        var exception = Assert.Throws<DomainException>(() => Appointment.Book(
            "brn_1", "cus_1", "pet_1", "svc_1", "stf_1", Now.AddHours(4), Now.AddHours(3)));

        Assert.Equal("invalid_appointment", exception.Code);
    }

    [Fact]
    public void Confirming_twice_is_not_an_error()
    {
        // A reminder link opened twice, or a LINE message re-tapped, must not answer 409.
        var appointment = Booked();

        appointment.Confirm(Now);
        var confirmedAt = appointment.ConfirmedAt;

        appointment.Confirm(Now.AddMinutes(5));

        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        Assert.Equal(confirmedAt, appointment.ConfirmedAt);
    }

    [Fact]
    public void A_walk_in_can_check_in_straight_from_booked()
    {
        var appointment = Booked();

        appointment.CheckIn(Now);

        Assert.Equal(AppointmentStatus.CheckedIn, appointment.Status);
        // Arriving implies confirming; the counter should not have to do both.
        Assert.NotNull(appointment.ConfirmedAt);
    }

    [Fact]
    public void A_cancelled_appointment_releases_its_slot()
    {
        var appointment = Booked();

        appointment.Cancel(Now, "usr_1", "ติดธุระ");

        Assert.False(appointment.OccupiesSlot);
        Assert.Equal("usr_1", appointment.CancelledByUserId);
        Assert.Equal("ติดธุระ", appointment.CancellationReason);
    }

    [Fact]
    public void A_cancelled_appointment_cannot_be_checked_in()
    {
        var appointment = Booked();
        appointment.Cancel(Now, "usr_1", null);

        var exception = Assert.Throws<StateConflictException>(() => appointment.CheckIn(Now));

        Assert.Equal(nameof(AppointmentStatus.Cancelled), exception.CurrentState);
        Assert.Equal("CheckIn", exception.AttemptedTransition);
    }

    [Fact]
    public void A_checked_in_appointment_cannot_be_marked_no_show()
    {
        // The pet is in the building. "Did not show" is no longer an available truth.
        var appointment = Booked();
        appointment.CheckIn(Now);

        Assert.Throws<StateConflictException>(() => appointment.MarkNoShow(Now));
    }

    [Fact]
    public void A_cancelled_appointment_cannot_be_rescheduled()
    {
        var appointment = Booked();
        appointment.Cancel(Now, "usr_1", null);

        Assert.Throws<StateConflictException>(
            () => appointment.Reschedule(Now.AddDays(1), Now.AddDays(1).AddMinutes(30), "stf_1"));
    }

    [Fact]
    public void Rescheduling_clears_the_confirmation()
    {
        // The customer agreed to a time, not to an appointment. Carrying the confirmation across
        // would show the clinic a confirmed slot nobody had accepted.
        var appointment = Booked();
        appointment.Confirm(Now);

        appointment.Reschedule(Now.AddDays(1), Now.AddDays(1).AddMinutes(30), "stf_2");

        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
        Assert.Null(appointment.ConfirmedAt);
        Assert.Equal("stf_2", appointment.StaffId);
    }

    [Fact]
    public void Cancelling_twice_keeps_the_first_reason()
    {
        var appointment = Booked();
        appointment.Cancel(Now, "usr_1", "เหตุผลแรก");

        appointment.Cancel(Now.AddMinutes(1), "usr_2", "เหตุผลที่สอง");

        Assert.Equal("usr_1", appointment.CancelledByUserId);
        Assert.Equal("เหตุผลแรก", appointment.CancellationReason);
    }

    [Theory]
    [InlineData(AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.NoShow)]
    public void Terminal_states_have_no_way_out(AppointmentStatus terminal) =>
        Assert.Empty(AppointmentTransitions.NextFrom(terminal));

    [Theory]
    [InlineData(AppointmentStatus.Booked)]
    [InlineData(AppointmentStatus.Confirmed)]
    [InlineData(AppointmentStatus.CheckedIn)]
    [InlineData(AppointmentStatus.InConsultation)]
    public void Live_states_hold_the_slot(AppointmentStatus status) =>
        Assert.True(AppointmentTransitions.OccupiesSlot(status));

    [Theory]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.NoShow)]
    [InlineData(AppointmentStatus.Completed)]
    public void Finished_states_release_the_slot(AppointmentStatus status) =>
        Assert.False(AppointmentTransitions.OccupiesSlot(status));

    [Fact]
    public void The_occupying_list_and_the_predicate_cannot_disagree()
    {
        // The repository pushes OccupyingStatuses into SQL while the aggregate uses OccupiesSlot.
        // If those two ever diverge, the collision query stops seeing appointments that are still
        // holding their time — which is a double booking, silently.
        foreach (var status in Enum.GetValues<AppointmentStatus>())
        {
            Assert.Equal(
                AppointmentTransitions.OccupyingStatuses.Contains(status),
                AppointmentTransitions.OccupiesSlot(status));
        }
    }
}
