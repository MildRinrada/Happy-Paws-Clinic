using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Auditing;

/// <summary>
/// The audit registry is a contract, so it is asserted like one.
/// <para>
/// The list in <see cref="AuditActions"/> is this project's answer to a gap in the API reference,
/// which reads audit logs back without ever saying what must be written to them. These tests exist
/// so the list cannot quietly drift out of agreement with itself.
/// </para>
/// </summary>
public class AuditActionsTests
{
    [Fact]
    public void Every_access_action_is_also_a_known_action()
    {
        // Otherwise the access log could select for an action that AuditEntry.Write refuses to
        // record, and the endpoint would return an empty page forever with nothing to explain it.
        Assert.All(AuditActions.AccessActions, action => Assert.True(AuditActions.IsKnown(action)));
    }

    [Fact]
    public void Actions_are_namespaced_so_the_log_can_be_filtered_by_area()
    {
        Assert.All(AuditActions.All, action => Assert.Contains('.', action));
    }

    [Fact]
    public void An_action_outside_the_registry_cannot_be_written()
    {
        // A typo'd action name would create a category nobody queries, which is the same as not
        // auditing that action at all.
        Assert.Throws<DomainException>(() =>
            AuditEntry.Write("consultation.opened_typo", AuditResources.Consultation, "con_1", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void A_view_is_classified_as_access_and_a_change_is_not()
    {
        var view = AuditEntry.Write(
            AuditActions.RecordViewed, AuditResources.MedicalRecord, "rec_1", DateTimeOffset.UtcNow);
        var change = AuditEntry.Write(
            AuditActions.RecordAmended, AuditResources.MedicalRecord, "rec_1", DateTimeOffset.UtcNow);

        // "Who changed this record" and "who looked at this patient's file" are different
        // questions, and the contract gives them different endpoints.
        Assert.True(view.IsAccess);
        Assert.False(change.IsAccess);
    }
}
