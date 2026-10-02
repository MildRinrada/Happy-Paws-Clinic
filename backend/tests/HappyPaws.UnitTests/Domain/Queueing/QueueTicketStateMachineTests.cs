using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.UnitTests.Domain.Queueing;

/// <summary>
/// The queue-ticket state machine, including the edges that must not exist.
/// </summary>
public class QueueTicketStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 3, 0, 0, TimeSpan.Zero);

    private static QueueTicket Waiting(int sequence = 1) =>
        QueueTicket.IssueWalkIn("brn_1", "pet_1", "cus_1", "svc_1", sequence, new DateOnly(2026, 8, 21), Now);

    private static QueueTicket FromAppointment()
    {
        var appointment = Appointment.Book(
            "brn_1", "cus_1", "pet_1", "svc_1", "stf_1", Now.AddHours(1), Now.AddHours(1).AddMinutes(30));

        appointment.CheckIn(Now);

        return QueueTicket.IssueForAppointment(appointment, 14, new DateOnly(2026, 8, 21), Now);
    }

    [Fact]
    public void A_new_ticket_is_waiting_and_live()
    {
        var ticket = Waiting();

        Assert.Equal(QueueTicketStatus.Waiting, ticket.Status);
        Assert.True(ticket.IsLive);
        Assert.Equal("A001", ticket.Number);
        Assert.Equal(1, ticket.Sequence);
    }

    [Fact]
    public void A_ticket_from_an_appointment_inherits_its_patient_and_vet()
    {
        var ticket = FromAppointment();

        Assert.Equal("A014", ticket.Number);
        Assert.Equal("pet_1", ticket.PetId);
        Assert.Equal("stf_1", ticket.StaffId);
        Assert.NotNull(ticket.AppointmentId);
    }

    [Fact]
    public void Calling_records_who_called_and_when()
    {
        var ticket = Waiting();

        ticket.Call("stf_7", Now);

        Assert.Equal(QueueTicketStatus.Called, ticket.Status);
        Assert.Equal("stf_7", ticket.CalledByStaffId);
        Assert.Equal(Now, ticket.CalledAt);
    }

    [Fact]
    public void A_skipped_ticket_can_be_called_again()
    {
        // "เลื่อนไปท้ายแถว" — moved to the back, not removed. An owner who stepped out for two
        // minutes must not have to queue again from the counter.
        var ticket = Waiting();

        ticket.Skip(Now);

        Assert.Equal(QueueTicketStatus.Skipped, ticket.Status);
        Assert.True(ticket.IsLive);

        ticket.Call("stf_7", Now.AddMinutes(5));

        Assert.Equal(QueueTicketStatus.Called, ticket.Status);
    }

    [Fact]
    public void Each_skip_moves_the_ticket_further_back()
    {
        var ticket = Waiting();

        ticket.Skip(Now);
        var first = ticket.SkippedAt;

        ticket.Call("stf_7", Now.AddMinutes(1));
        ticket.Skip(Now.AddMinutes(2));

        Assert.Equal(2, ticket.SkipCount);
        // The ordering key is when you were *last* passed over.
        Assert.True(ticket.SkippedAt > first);
    }

    [Fact]
    public void A_ticket_already_at_the_back_cannot_be_skipped_again()
    {
        // Nothing to skip, and a double-press must not inflate the count the clinic reads as
        // "how many times did this owner fail to come forward".
        var ticket = Waiting();
        ticket.Skip(Now);

        Assert.Throws<StateConflictException>(() => ticket.Skip(Now.AddMinutes(1)));
        Assert.Equal(1, ticket.SkipCount);
    }

    [Fact]
    public void A_called_ticket_can_be_skipped_when_nobody_comes_forward()
    {
        var ticket = Waiting();
        ticket.Call("stf_7", Now);

        ticket.Skip(Now.AddMinutes(2));

        Assert.Equal(QueueTicketStatus.Skipped, ticket.Status);
    }

    [Fact]
    public void A_ticket_cannot_enter_a_room_without_being_called()
    {
        var ticket = Waiting();

        var exception = Assert.Throws<StateConflictException>(() => ticket.EnterRoom(Now));

        Assert.Equal(nameof(QueueTicketStatus.Waiting), exception.CurrentState);
    }

    [Fact]
    public void A_served_ticket_is_finished_and_off_the_board()
    {
        var ticket = Waiting();
        ticket.Call("stf_7", Now);
        ticket.EnterRoom(Now.AddMinutes(1));
        ticket.MarkServed(Now.AddMinutes(20));

        Assert.Equal(QueueTicketStatus.Served, ticket.Status);
        Assert.False(ticket.IsLive);
        Assert.Throws<StateConflictException>(() => ticket.Call("stf_7", Now.AddMinutes(30)));
    }

    [Fact]
    public void A_departed_ticket_cannot_be_called_back()
    {
        var ticket = Waiting();
        ticket.MarkLeft(Now);

        Assert.False(ticket.IsLive);
        Assert.Throws<StateConflictException>(() => ticket.Call("stf_7", Now.AddMinutes(1)));
    }

    [Fact]
    public void Assigning_keeps_the_status_so_a_patient_can_change_room_mid_visit()
    {
        var ticket = Waiting();
        ticket.Call("stf_7", Now);
        ticket.EnterRoom(Now.AddMinutes(1));

        ticket.Assign(staffId: null, roomId: "room_3");

        Assert.Equal(QueueTicketStatus.InRoom, ticket.Status);
        Assert.Equal("room_3", ticket.RoomId);
    }

    [Fact]
    public void A_null_side_of_an_assignment_leaves_that_side_alone()
    {
        var ticket = Waiting();

        ticket.Assign("stf_2", "room_1");
        ticket.Assign(staffId: null, roomId: "room_2");

        Assert.Equal("stf_2", ticket.StaffId);
        Assert.Equal("room_2", ticket.RoomId);
    }

    [Fact]
    public void A_finished_ticket_cannot_be_reassigned()
    {
        var ticket = Waiting();
        ticket.MarkLeft(Now);

        Assert.Throws<StateConflictException>(() => ticket.Assign("stf_2", "room_1"));
    }

    [Theory]
    [InlineData(QueueTicketStatus.Served)]
    [InlineData(QueueTicketStatus.Left)]
    public void Terminal_states_have_no_way_out(QueueTicketStatus terminal) =>
        Assert.Empty(QueueTicketTransitions.NextFrom(terminal));

    [Fact]
    public void The_live_list_and_the_predicate_cannot_disagree()
    {
        // The board query pushes LiveStatuses into SQL while the ticket uses IsLive. A divergence
        // means a patient vanishing from the board while still waiting.
        foreach (var status in Enum.GetValues<QueueTicketStatus>())
        {
            Assert.Equal(
                QueueTicketTransitions.LiveStatuses.Contains(status),
                QueueTicketTransitions.IsLive(status));
        }
    }

    [Fact]
    public void Every_awaiting_call_status_is_also_a_live_status()
    {
        // A ticket that is waiting to be called but not on the board would be invisible to the
        // counter and uncountable in "how many ahead of me".
        Assert.All(
            QueueTicketTransitions.AwaitingCallStatuses,
            status => Assert.Contains(status, QueueTicketTransitions.LiveStatuses));
    }
}
