using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Staff;

namespace HappyPaws.Application.Queueing;

/// <summary>
/// The vet put on a queue ticket is a vet who can see the patient: an active staff member, a
/// veterinarian by staff type or department, working at the ticket's branch (assigned to it, or
/// bound to no branch).
/// <para>
/// Before this, <c>POST /staff/queue/{id}/assign</c> and the walk-in ticket stored whatever
/// <c>staff_id</c> they were sent, so a typo, a receptionist's id or a vet who left last month
/// went onto the board and the waiting-room screen. A bug fix rather than a change of shape: the
/// refusal is the ordinary <c>422</c> at <c>staff_id</c>, like the room check beside it
/// (<c>docs/contract-extensions.md § 31</c>).
/// </para>
/// </summary>
public sealed class TicketVetGuard(IStaffRepository staff)
{
    public async Task EnsureVetAtBranchAsync(string staffId, string branchId, CancellationToken cancellationToken = default)
    {
        var row = await staff.FindRowAsync(staffId, cancellationToken);

        if (row is null
            || !row.Membership.IsActive
            || !row.Membership.IsVeterinarian
            || !row.Membership.WorksAt(branchId))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["staff_id"] = ["ไม่พบสัตวแพทย์คนนี้ในสาขา หรือบัญชีถูกปิดแล้ว"],
            });
        }
    }
}
