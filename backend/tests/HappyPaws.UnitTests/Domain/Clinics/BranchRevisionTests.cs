using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Clinics;

/// <summary>Editing a branch (<c>docs/contract-extensions.md § 51</c>): partial, and never the code.</summary>
public class BranchRevisionTests
{
    private static Branch Sukhumvit() =>
        Branch.Create(
            "sukhumvit39",
            "สาขาสุขุมวิท 39",
            examRooms: ["room_1", "room_2"],
            address: "39 ซอยสุขุมวิท 39",
            latitude: 13.73,
            longitude: 100.57);

    [Fact]
    public void Only_what_is_given_changes()
    {
        var branch = Sukhumvit();

        branch.Revise(name: "  สาขาพร้อมพงษ์  ");

        Assert.Equal("สาขาพร้อมพงษ์", branch.Name);
        Assert.Equal("sukhumvit39", branch.Code);
        Assert.Equal("39 ซอยสุขุมวิท 39", branch.Address);
        Assert.Equal(13.73, branch.Latitude);
        Assert.Equal(["room_1", "room_2"], branch.ExamRooms);
    }

    [Fact]
    public void An_empty_address_clears_it()
    {
        var branch = Sukhumvit();

        branch.Revise(address: " ");

        Assert.Null(branch.Address);
    }

    [Fact]
    public void A_new_pin_needs_both_halves()
    {
        var branch = Sukhumvit();

        // Half a coordinate is a pin in the ocean, on an edit as on creation.
        Assert.Throws<DomainException>(() => branch.Revise(latitude: 14.0));
        Assert.Equal(13.73, branch.Latitude);

        branch.Revise(latitude: 13.7325, longitude: 100.5786);

        Assert.Equal(13.7325, branch.Latitude);
        Assert.Equal(100.5786, branch.Longitude);
    }

    [Fact]
    public void A_blank_name_is_refused()
    {
        var branch = Sukhumvit();

        Assert.Throws<DomainException>(() => branch.Revise(name: "   "));
        Assert.Equal("สาขาสุขุมวิท 39", branch.Name);
    }

    [Fact]
    public void The_rooms_sent_are_the_whole_list()
    {
        var branch = Sukhumvit();

        branch.Revise(examRooms: ["room_2", " room_3 ", "room_3", ""]);

        Assert.Equal(["room_2", "room_3"], branch.ExamRooms);
    }

    [Fact]
    public void The_rooms_an_edit_would_remove_are_read_the_way_they_would_be_stored()
    {
        var branch = Sukhumvit();

        // " room_1 " is room_1 once trimmed, so only room_2 would go.
        Assert.Equal(["room_2"], branch.RoomsRemovedBy([" room_1 ", "room_9"]));
        Assert.Empty(branch.RoomsRemovedBy(["room_1", "room_2", "room_3"]));
        Assert.Equal(["room_1", "room_2"], branch.RoomsRemovedBy([]));
    }
}
