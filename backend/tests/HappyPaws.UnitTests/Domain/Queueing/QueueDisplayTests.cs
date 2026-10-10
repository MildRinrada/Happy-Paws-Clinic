using HappyPaws.Domain.Common;
using HappyPaws.Domain.Queueing;
using HappyPaws.Domain.Staff;

namespace HappyPaws.UnitTests.Domain.Queueing;

/// <summary>
/// A waiting-room screen before the database is involved: its setup code, its token's shape and its
/// states (<c>docs/contract-extensions.md § 30</c>); and the vet rule a queue ticket's vet must meet
/// (§ 31).
/// </summary>
public class QueueDisplayTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_display_shows_its_code_once_and_keeps_only_the_hash()
    {
        var (display, code) = QueueDisplay.Create(" ทีวีห้องรอ ", "brn_1", expiresAt: null, Now, "stf_1");

        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}$", code);
        Assert.Equal("ทีวีห้องรอ", display.Name);
        Assert.Equal(QueueDisplay.Hash(QueueDisplay.NormaliseCode(code)!), display.SetupCodeHash);
        Assert.NotEqual(code, display.SetupCodeHash);
        Assert.Null(display.TokenHash);
        Assert.Equal(Now + QueueDisplay.SetupLifetime, display.SetupExpiresAt);
        Assert.StartsWith("dsp_", display.Id);
    }

    [Fact]
    public void A_typed_code_is_read_loosely_and_anything_else_is_not_a_code()
    {
        Assert.Equal("ABCDEFGH", QueueDisplay.NormaliseCode("abcd-efgh"));
        Assert.Equal("ABCDEFGH", QueueDisplay.NormaliseCode(" ABCD EFGH "));
        Assert.Null(QueueDisplay.NormaliseCode("ABCD-EFG"));
        Assert.Null(QueueDisplay.NormaliseCode("ABCD-EFG0"));
        Assert.Null(QueueDisplay.NormaliseCode(null));
    }

    [Fact]
    public void The_code_can_be_claimed_only_within_its_half_hour()
    {
        var (display, _) = QueueDisplay.Create("ทีวี", "brn_1", null, Now, "stf_1");

        Assert.True(display.CanBeClaimedAt(Now.AddMinutes(29)));
        Assert.False(display.CanBeClaimedAt(Now.AddMinutes(30)));
        Assert.Equal("pending_setup", display.StateAt(Now));
        Assert.Equal("setup_expired", display.StateAt(Now.AddMinutes(31)));
        Assert.False(display.IsUsableAt(Now));
    }

    [Fact]
    public void An_end_date_must_be_in_the_future_and_within_two_years()
    {
        Assert.Throws<DomainException>(() => QueueDisplay.Create("ทีวี", "brn_1", Now.AddMinutes(-1), Now, "stf_1"));
        Assert.Throws<DomainException>(() => QueueDisplay.Create("ทีวี", "brn_1", Now.AddDays(800), Now, "stf_1"));
        Assert.Throws<DomainException>(() => QueueDisplay.Create(" ", "brn_1", null, Now, "stf_1"));

        var (display, _) = QueueDisplay.Create("ทีวี", "brn_1", Now.AddDays(30), Now, "stf_1");
        Assert.Equal("expired", display.StateAt(Now.AddDays(31)));
        Assert.False(display.CanBeClaimedAt(Now.AddDays(31)));
    }

    [Fact]
    public void Tokens_are_long_random_and_recognisable()
    {
        var first = QueueDisplay.NewToken();
        var second = QueueDisplay.NewToken();

        Assert.True(QueueDisplay.LooksLikeToken(first));
        Assert.NotEqual(first, second);
        Assert.False(QueueDisplay.LooksLikeToken("hp_k7m2qx9ab4_" + new string('a', 43)));
        Assert.False(QueueDisplay.LooksLikeToken("hpdsp_short"));
    }

    [Theory]
    [InlineData("veterinarian", null, true)]
    [InlineData("vet_specialist", "veterinary", true)]
    [InlineData("receptionist", null, false)]
    [InlineData("vet_assistant", null, false)]
    public void A_vet_is_the_vet_type_or_the_veterinary_department(string staffType, string? department, bool expected)
    {
        var member = StaffMembership.Create("usr_1", staffType, [], ["brn_1"], department: department);

        Assert.Equal(expected, member.IsVeterinarian);
    }

    [Fact]
    public void A_member_works_at_their_branches_or_everywhere_when_bound_to_none()
    {
        var bound = StaffMembership.Create("usr_1", "veterinarian", [], ["brn_1"]);
        var roaming = StaffMembership.Create("usr_2", "veterinarian", [], []);

        Assert.True(bound.WorksAt("brn_1"));
        Assert.False(bound.WorksAt("brn_2"));
        Assert.True(roaming.WorksAt("brn_2"));
    }
}
