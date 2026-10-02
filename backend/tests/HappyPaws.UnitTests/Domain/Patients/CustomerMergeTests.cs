using HappyPaws.Domain.Common;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Patients;

namespace HappyPaws.UnitTests.Domain.Patients;

/// <summary>
/// What a merge does to the two records themselves, and to a carer link they both hold.
/// </summary>
public class CustomerMergeTests
{
    private static Customer Someone(string name = "คุณสมชาย") => Customer.Create(name, "0812345678");

    [Fact]
    public void A_merged_record_becomes_a_tombstone_pointing_at_the_survivor()
    {
        var surviving = Someone();
        var duplicate = Someone("สมชาย (ซ้ำ)");

        duplicate.MergeInto(surviving.Id);

        // Kept, not deleted: an id on last year's receipt still resolves, and resolves to an answer
        // that says where the record went.
        Assert.True(duplicate.IsMerged);
        Assert.False(duplicate.IsActive);
        Assert.Equal(surviving.Id, duplicate.MergedIntoCustomerId);
    }

    [Fact]
    public void A_record_cannot_be_merged_twice_or_into_itself()
    {
        var duplicate = Someone();

        Assert.Throws<DomainException>(() => duplicate.MergeInto(duplicate.Id));

        duplicate.MergeInto("cus_survivor");

        Assert.Throws<DomainException>(() => duplicate.MergeInto("cus_other"));
    }

    [Fact]
    public void The_survivor_takes_the_portal_account_when_it_has_none()
    {
        var surviving = Someone();
        var duplicate = Someone();
        duplicate.LinkAccount("usr_8fA2");

        surviving.AdoptAccountFrom(duplicate);

        Assert.Equal("usr_8fA2", surviving.UserId);

        // Cleared on the duplicate, because the partial-unique index on user_id would otherwise
        // refuse the write — and because one person having two customer rows pointing at one login
        // is the state the merge exists to end.
        Assert.Null(duplicate.UserId);
    }

    [Fact]
    public void An_account_the_survivor_already_has_is_left_alone()
    {
        var surviving = Someone();
        surviving.LinkAccount("usr_survivor");

        var duplicate = Someone();
        duplicate.LinkAccount("usr_duplicate");

        surviving.AdoptAccountFrom(duplicate);

        // Two logins for one person is not something to resolve silently. The handler refuses the
        // merge outright; this method must not quietly pick a winner if it is ever reached.
        Assert.Equal("usr_survivor", surviving.UserId);
        Assert.Equal("usr_duplicate", duplicate.UserId);
    }

    [Fact]
    public void Folding_two_carer_links_keeps_the_wider_access()
    {
        var survivorLink = PetOwnership.Create("pet_luna", "cus_survivor", isPrimary: false, mayViewHistory: false);
        var duplicateLink = PetOwnership.Create("pet_luna", "cus_duplicate", isPrimary: true, mayViewHistory: true);

        survivorLink.Absorb(duplicateLink);

        // Taking the narrower would remove access the person already had, which is the one thing a
        // merge must never do — it tidies records, it does not revoke anything.
        Assert.True(survivorLink.IsPrimary);
        Assert.True(survivorLink.MayViewHistory);
    }

    [Fact]
    public void The_primary_carer_cannot_simply_be_removed()
    {
        var primary = PetOwnership.Create("pet_luna", "cus_1", isPrimary: true);

        // Removing the last carer would orphan the record and, with it, every notification and
        // invoice that hangs off the owner.
        Assert.Throws<DomainException>(() => primary.Revoke(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Withdrawing_a_co_owner_closes_the_link_rather_than_erasing_it()
    {
        var at = DateTimeOffset.UtcNow;
        var link = PetOwnership.Create("pet_luna", "cus_2");

        link.Revoke(at);

        // "ถอนสิทธิ์เข้าถึง ไม่ลบประวัติ" — who could see what, and until when, stays answerable.
        Assert.False(link.IsActive);
        Assert.Equal(at, link.RevokedAt);
    }
}
