using HappyPaws.Domain.Referrals;

namespace HappyPaws.UnitTests.Domain.Referrals;

/// <summary>
/// The customer's referral code: eight characters that are easy to read aloud, drawn at random, and
/// matched however they are typed (<c>docs/contract-extensions.md § 39</c>).
/// </summary>
public class ReferralCodeTests
{
    [Theory]
    [InlineData('0')]
    [InlineData('O')]
    [InlineData('1')]
    [InlineData('I')]
    [InlineData('L')]
    public void The_alphabet_leaves_out_characters_that_are_misread(char confusable)
    {
        Assert.DoesNotContain(confusable, ReferralCode.Alphabet);
    }

    [Fact]
    public void The_alphabet_has_no_repeats_and_only_upper_case_letters_and_digits()
    {
        Assert.Equal(ReferralCode.Alphabet.Length, ReferralCode.Alphabet.Distinct().Count());
        Assert.All(ReferralCode.Alphabet, c => Assert.True(char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c)));
    }

    [Fact]
    public void A_generated_code_is_eight_characters_of_the_alphabet()
    {
        for (var i = 0; i < 500; i++)
        {
            var code = ReferralCode.Generate();

            Assert.Equal(ReferralCode.Length, code.Length);
            Assert.All(code, c => Assert.Contains(c, ReferralCode.Alphabet));
        }
    }

    [Fact]
    public void Generated_codes_do_not_repeat_and_use_the_whole_alphabet()
    {
        var codes = Enumerable.Range(0, 20_000).Select(_ => ReferralCode.Generate()).ToList();

        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ReferralCode.Alphabet.Length, codes.SelectMany(code => code).Distinct().Count());
    }

    [Fact]
    public void A_code_for_a_customer_belongs_to_that_customer_and_carries_a_referral_code_id()
    {
        var code = ReferralCode.For("cus_1");

        Assert.Equal("cus_1", code.CustomerId);
        Assert.StartsWith("rfc_", code.Id);
        Assert.Equal(code.Code, ReferralCode.Normalize(code.Code));
    }

    [Theory]
    [InlineData("7kx4pm2q", "7KX4PM2Q")]
    [InlineData(" 7KX4-PM2Q ", "7KX4PM2Q")]
    [InlineData("7KX4 PM2Q", "7KX4PM2Q")]
    public void Typed_codes_are_matched_in_upper_case_without_spaces_or_dashes(string typed, string stored)
    {
        Assert.Equal(stored, ReferralCode.Normalize(typed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("7KX4PM2")]
    [InlineData("7KX4PM2QQ")]
    [InlineData("7KX4PM20")]
    [InlineData("7KX4PM2O")]
    [InlineData("7KX4PM21")]
    [InlineData("7KX4PM2I")]
    [InlineData("7KX4PM2!")]
    [InlineData("7KX4PM2Q'; DROP TABLE referrals; --")]
    public void Anything_that_is_not_eight_characters_of_the_alphabet_is_no_code(string? typed)
    {
        Assert.Null(ReferralCode.Normalize(typed));
    }
}
