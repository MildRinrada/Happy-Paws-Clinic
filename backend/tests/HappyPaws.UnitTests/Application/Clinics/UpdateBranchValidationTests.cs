using HappyPaws.Application.Clinics.UpdateBranch;

namespace HappyPaws.UnitTests.Application.Clinics;

/// <summary>
/// What <c>PATCH /clinic/branches/{id}</c> accepts before anything is loaded
/// (<c>docs/contract-extensions.md § 51</c>): the limits of opening a branch, on what was sent.
/// </summary>
public class UpdateBranchValidationTests
{
    private static bool Valid(UpdateBranchCommand command) => new UpdateBranchValidator().Validate(command).IsValid;

    [Fact]
    public void An_edit_that_sends_nothing_is_valid()
    {
        Assert.True(Valid(new UpdateBranchCommand("brn_a")));
    }

    [Fact]
    public void An_empty_address_is_how_it_is_cleared()
    {
        Assert.True(Valid(new UpdateBranchCommand("brn_a", Address: "")));
    }

    [Theory]
    [InlineData(13.7, 100.5, true)]
    [InlineData(13.7, null, false)]
    [InlineData(null, 100.5, false)]
    [InlineData(-91.0, 100.5, false)]
    [InlineData(13.7, 181.0, false)]
    public void A_pin_is_both_halves_and_on_the_earth(double? latitude, double? longitude, bool valid)
    {
        Assert.Equal(valid, Valid(new UpdateBranchCommand("brn_a", Latitude: latitude, Longitude: longitude)));
    }

    [Fact]
    public void A_name_sent_must_not_be_empty_or_too_long()
    {
        Assert.False(Valid(new UpdateBranchCommand("brn_a", Name: "")));
        Assert.False(Valid(new UpdateBranchCommand("brn_a", Name: new string('ก', 201))));
        Assert.True(Valid(new UpdateBranchCommand("brn_a", Name: "สาขาทองหล่อ")));
    }

    [Fact]
    public void Every_room_has_a_name_of_at_most_60_characters()
    {
        Assert.True(Valid(new UpdateBranchCommand("brn_a", ExamRooms: [])));
        Assert.False(Valid(new UpdateBranchCommand("brn_a", ExamRooms: ["room_1", ""])));
        Assert.False(Valid(new UpdateBranchCommand("brn_a", ExamRooms: [new string('x', 61)])));
    }
}
