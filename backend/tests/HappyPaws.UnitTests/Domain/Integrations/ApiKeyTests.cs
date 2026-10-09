using HappyPaws.Domain.Access;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.UnitTests.Domain.Integrations;

/// <summary>
/// An API key: shown once, stored as a hash, identified by its prefix, always ending, and never
/// able to carry a scope that needs a second factor or that hands out more access.
/// </summary>
public class ApiKeyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_key_is_shown_once_and_only_its_hash_is_kept()
    {
        var (key, plaintext) = Issue();

        Assert.StartsWith("hp_" + key.Prefix + "_", plaintext, StringComparison.Ordinal);
        Assert.Equal(ApiKey.Hash(plaintext), key.SecretHash);
        Assert.Equal(64, key.SecretHash.Length);
        Assert.DoesNotContain(plaintext[^20..], key.SecretHash, StringComparison.Ordinal);
        Assert.True(key.Matches(plaintext));
        Assert.False(key.Matches(plaintext[..^1] + (plaintext[^1] == 'a' ? 'b' : 'a')));
    }

    [Fact]
    public void Two_keys_never_share_a_prefix_or_a_secret()
    {
        var (first, a) = Issue();
        var (second, b) = Issue();

        Assert.NotEqual(first.Prefix, second.Prefix);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void The_prefix_is_read_from_a_well_formed_key_only()
    {
        var (key, plaintext) = Issue();

        Assert.Equal(key.Prefix, ApiKey.PrefixOf(plaintext));
        Assert.Null(ApiKey.PrefixOf("hp_short"));
        Assert.Null(ApiKey.PrefixOf("eyJhbGciOiJIUzI1NiJ9.e30.sig"));
        Assert.Null(ApiKey.PrefixOf(plaintext + "x"));
    }

    [Fact]
    public void A_key_stops_at_its_expiry()
    {
        var (key, _) = Issue(Now.AddDays(1));

        Assert.True(key.IsUsableAt(Now));
        Assert.False(key.IsUsableAt(Now.AddDays(1)));
    }

    [Fact]
    public void Expiry_must_be_in_the_future_and_within_a_year()
    {
        Assert.Throws<DomainException>(() => Issue(Now));
        Assert.Throws<DomainException>(() => Issue(Now.AddDays(400)));
    }

    [Fact]
    public void Last_used_is_written_at_most_once_a_minute()
    {
        var (key, _) = Issue();

        Assert.True(key.NeedsLastUsedUpdate(Now));
    }

    [Theory]
    [InlineData("record:read")]
    [InlineData("consult:write")]
    [InlineData("rx:dispense")]
    [InlineData("payment:refund")]
    [InlineData("role:manage")]
    [InlineData("user:manage")]
    [InlineData("staff:manage")]
    [InlineData("integration:manage")]
    [InlineData("settings:danger")]
    [InlineData("*")]
    [InlineData("appointment:*")]
    [InlineData("portal")]
    [InlineData("nothing:real")]
    public void A_key_may_not_carry_these(string scope)
    {
        Assert.False(ApiKeyScopeRules.MayCarry(scope));
    }

    [Theory]
    [InlineData("appointment:read")]
    [InlineData("customer:read")]
    [InlineData("customer:write")]
    [InlineData("invoice:write")]
    [InlineData("inventory:read")]
    public void A_key_may_carry_ordinary_scopes(string scope)
    {
        Assert.True(ApiKeyScopeRules.MayCarry(scope));
        Assert.Contains(scope, ApiKeyScopeRules.Grantable);
    }

    [Fact]
    public void Every_second_factor_scope_is_withheld_from_keys()
    {
        Assert.All(SecondFactorScopes.All, scope => Assert.DoesNotContain(scope, ApiKeyScopeRules.Grantable));
    }

    private static (ApiKey Key, string Plaintext) Issue(DateTimeOffset? expiresAt = null) =>
        ApiKey.Issue("ระบบบัญชี", ["appointment:read"], [], expiresAt ?? Now.AddDays(90), Now, "usr_1", "stf_1");
}
