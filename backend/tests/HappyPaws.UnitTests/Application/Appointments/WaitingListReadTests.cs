using HappyPaws.Application.Appointments.Dtos;
using HappyPaws.Application.Appointments.ListWaitingList;
using HappyPaws.Application.Appointments.RemoveFromWaitingList;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.UnitTests.Application.Appointments;

/// <summary>
/// The pure parts of reading and removing waiting-list entries (<c>docs/contract-extensions.md § 23</c>):
/// the worked-out status and the request rules.
/// </summary>
public class WaitingListReadTests
{
    private static readonly DateOnly Today = new(2026, 10, 9);

    private static WaitingListEntry Entry(DateOnly latest) =>
        WaitingListEntry.Add("cus_1", "pet_1", "svc_1", Today, latest, Today, "stf_1");

    [Fact]
    public void An_entry_is_waiting_through_its_last_day_and_lapsed_after()
    {
        var entry = Entry(Today.AddDays(3));

        Assert.Equal("waiting", WaitingListItemDto.From(entry, "ลูน่า", "ตรวจสุขภาพ", Today.AddDays(3)).Status);
        Assert.Equal("lapsed", WaitingListItemDto.From(entry, "ลูน่า", "ตรวจสุขภาพ", Today.AddDays(4)).Status);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("waiting", true)]
    [InlineData("lapsed", true)]
    [InlineData("withdrawn", false)]
    [InlineData("WAITING", false)]
    public void Only_the_two_statuses_are_accepted(string? status, bool valid)
    {
        var result = new ListWaitingListValidator().Validate(new ListWaitingListQuery(Status: status));

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Removing_needs_an_entry_id()
    {
        Assert.False(new RemoveFromWaitingListValidator().Validate(new RemoveFromWaitingListCommand("")).IsValid);
        Assert.True(new RemoveFromWaitingListValidator().Validate(new RemoveFromWaitingListCommand("wtl_1")).IsValid);
    }
}
