using HappyPaws.Application.Identity;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Identity;

namespace HappyPaws.UnitTests.Domain.Identity;

public class AccountSelfServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private static UserAccount Account() => UserAccount.CreateStaff("คุณทดสอบ", "a@happypaws.test", "hash");

    [Fact]
    public void A_deactivated_account_stays_deactivated()
    {
        var account = Account();
        account.SetStatus(UserStatus.Deactivated, Now);

        Assert.Throws<StateConflictException>(() => account.SetStatus(UserStatus.Active, Now));
        Assert.Equal(UserStatus.Deactivated, account.Status);
    }

    [Fact]
    public void A_suspension_can_be_lifted_and_stops_older_credentials()
    {
        var account = Account();

        account.SetStatus(UserStatus.Suspended, Now);
        account.SetStatus(UserStatus.Active, Now.AddHours(1));

        Assert.True(account.CanAuthenticate);
        Assert.False(account.IsCredentialCurrent(Now.AddMinutes(-1)));
    }

    [Theory]
    [InlineData("no-at-sign")]
    [InlineData("@happypaws.test")]
    [InlineData("someone@")]
    [InlineData("two words@happypaws.test")]
    public void An_email_without_its_basic_shape_is_refused(string email) =>
        Assert.Throws<DomainException>(() => Account().ChangeEmail(email));

    [Fact]
    public void Email_is_stored_lower_case_and_phone_as_digits()
    {
        var account = Account();

        account.ChangeEmail("  New@HappyPaws.Test ");
        account.ChangePhone("081-234-5678");

        Assert.Equal("new@happypaws.test", account.Email);
        Assert.Equal("0812345678", account.PhoneNumber);
    }

    [Fact]
    public void Only_thai_and_english_are_offered() =>
        Assert.Throws<DomainException>(() => Account().UpdateProfile(language: "fr"));

    [Fact]
    public void A_second_factor_is_not_on_until_it_is_confirmed()
    {
        var account = Account();

        var first = account.BeginTwoFactorEnrolment(() => "SECRET1");
        var again = account.BeginTwoFactorEnrolment(() => "SECRET2");

        Assert.Equal("SECRET1", again);
        Assert.Equal(first, again);
        Assert.False(account.TwoFactorEnabled);

        account.CompleteTwoFactorEnrolment(["h1", "h2"]);

        Assert.True(account.TwoFactorEnabled);
        Assert.Equal("SECRET1", account.TwoFactorSecret);
        Assert.Null(account.PendingTwoFactorSecret);
    }

    [Fact]
    public void Confirming_without_starting_is_a_state_conflict() =>
        Assert.Throws<StateConflictException>(() => Account().CompleteTwoFactorEnrolment([]));

    [Fact]
    public void A_recovery_code_works_once()
    {
        var account = Account();
        account.BeginTwoFactorEnrolment(() => "SECRET");
        account.CompleteTwoFactorEnrolment([RecoveryCodes.Hash("abcde-12345")]);

        Assert.True(account.TryUseRecoveryCode(RecoveryCodes.Hash(" ABCDE-12345 ")));
        Assert.False(account.TryUseRecoveryCode(RecoveryCodes.Hash("abcde-12345")));
    }

    [Fact]
    public void Recovery_codes_are_distinct_and_recognisable()
    {
        var codes = RecoveryCodes.Generate();

        Assert.Equal(RecoveryCodes.Count, codes.Distinct().Count());
        Assert.All(codes, code => Assert.True(RecoveryCodes.LooksLikeOne(code)));
        Assert.False(RecoveryCodes.LooksLikeOne("123456"));
    }
}
