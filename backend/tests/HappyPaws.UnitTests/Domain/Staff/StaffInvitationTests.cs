using HappyPaws.Domain.Common;
using HappyPaws.Domain.Staff;

namespace HappyPaws.UnitTests.Domain.Staff;

public class StaffInvitationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 10, 0, 0, TimeSpan.FromHours(7));

    private static StaffInvitation Invitation() =>
        StaffInvitation.Send(
            "  Vet@Clinic.Test ",
            "สพ.ญ. มินท์",
            "veterinarian",
            ["rol_vet"],
            ["brn_a"],
            "ส.พ. 06120",
            tokenHash: "hash-1",
            invitedByStaffId: "stf_owner",
            Now);

    [Fact]
    public void The_link_lives_seven_days()
    {
        var invitation = Invitation();

        // The contract's "ลิงก์อายุ 7 วัน".
        Assert.True(invitation.IsLiveAt(Now.AddDays(7).AddSeconds(-1)));
        Assert.False(invitation.IsLiveAt(Now.AddDays(7)));
    }

    [Fact]
    public void The_address_is_stored_the_way_sign_in_will_look_it_up()
    {
        Assert.Equal("vet@clinic.test", Invitation().Email);
    }

    [Fact]
    public void Setting_up_the_second_factor_twice_shows_the_same_secret()
    {
        var invitation = Invitation();
        var minted = 0;

        var first = invitation.BeginTwoFactorSetup(() => $"secret-{++minted}");
        var second = invitation.BeginTwoFactorSetup(() => $"secret-{++minted}");

        // A page refresh must not invalidate the QR code already scanned.
        Assert.Equal(first, second);
        Assert.Equal(1, minted);
    }

    [Fact]
    public void Accepting_spends_the_invitation_and_drops_its_copy_of_the_secret()
    {
        var invitation = Invitation();
        invitation.BeginTwoFactorSetup(() => "secret");

        invitation.Accept("usr_new", Now.AddHours(1));

        Assert.True(invitation.IsAccepted);
        Assert.False(invitation.IsLiveAt(Now.AddHours(2)));
        Assert.Null(invitation.PendingTwoFactorSecret);
        Assert.Equal("usr_new", invitation.AcceptedUserId);
    }

    [Fact]
    public void An_accepted_invitation_cannot_be_accepted_or_reissued()
    {
        var invitation = Invitation();
        invitation.Accept("usr_new", Now);

        Assert.Throws<StateConflictException>(() => invitation.Accept("usr_other", Now));
        Assert.Throws<StateConflictException>(() =>
            invitation.Reissue("x", "vet", ["rol_vet"], [], null, "hash-2", "stf_owner", Now));
    }

    [Fact]
    public void Reissuing_replaces_the_link_restarts_the_clock_and_discards_half_finished_setup()
    {
        var invitation = Invitation();
        invitation.BeginTwoFactorSetup(() => "secret");

        var later = Now.AddDays(6);
        invitation.Reissue("สพ.ญ. มินท์", "veterinarian", ["rol_senior"], ["brn_b"], null, "hash-2", "stf_admin", later);

        Assert.Equal("hash-2", invitation.TokenHash);
        Assert.True(invitation.IsLiveAt(later.AddDays(6)));
        Assert.Null(invitation.PendingTwoFactorSecret);
        Assert.Equal(["rol_senior"], invitation.RoleIds);
        Assert.Equal(["brn_b"], invitation.BranchIds);
    }
}
