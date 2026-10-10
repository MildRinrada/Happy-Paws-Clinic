using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Referrals;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.ReferralScenario;

namespace HappyPaws.IntegrationTests.Api.Referrals;

/// <summary>
/// Refer a friend end to end (<c>docs/contract-extensions.md § 39</c>): a customer's code, the public
/// check that says only valid or not, a newcomer booking through <c>/book</c> with it, and the points
/// both get when the newcomer's first bill is paid, once, under the referrer's monthly cap.
/// </summary>
[Collection(nameof(ReferralCollection))]
public class ReferralTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<(SeededClinic Clinic, string Code)> ReferrerAsync()
    {
        await ConfigureAsync(fixture, cancellationToken: Token);
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var mine = await MyReferralAsync(fixture, clinic, Token);

        return (clinic, mine.GetProperty("code").GetString()!);
    }

    private async Task<Referral[]> StoredForReferrerAsync(string referrerId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<Referral>().AsNoTracking().Where(referral => referral.ReferrerCustomerId == referrerId).ToArrayAsync(Token);
    }

    [Fact]
    public async Task A_customers_code_is_made_on_the_first_read_and_never_changes()
    {
        await ConfigureAsync(fixture, cancellationToken: Token);
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var first = await MyReferralAsync(fixture, clinic, Token);
        var second = await MyReferralAsync(fixture, clinic, Token);
        var theirs = await MyReferralAsync(fixture, other, Token);

        var code = first.GetProperty("code").GetString()!;
        Assert.Equal(code, ReferralCode.Normalize(code));
        Assert.Equal(code, second.GetProperty("code").GetString());
        Assert.NotEqual(code, theirs.GetProperty("code").GetString());
        Assert.EndsWith($"/r/{code}", first.GetProperty("link").GetString());

        Assert.True(first.GetProperty("enabled").GetBoolean());
        Assert.Equal(0, first.GetProperty("invited").GetInt32());
        Assert.Equal(0, first.GetProperty("points_earned").GetInt32());
        Assert.Equal(5, first.GetProperty("monthly_cap").GetInt32());
        Assert.Equal(0, first.GetProperty("rewarded_this_month").GetInt32());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.Equal(1, await db.Set<ReferralCode>().CountAsync(row => row.CustomerId == clinic.CustomerId, Token));
    }

    [Fact]
    public async Task Two_first_reads_at_the_same_moment_share_one_code()
    {
        await ConfigureAsync(fixture, cancellationToken: Token);
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var reads = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => MyReferralAsync(fixture, clinic, Token)));

        Assert.Single(reads.Select(read => read.GetProperty("code").GetString()).Distinct());
    }

    [Fact]
    public async Task The_public_check_says_valid_or_not_and_nothing_else()
    {
        var (_, code) = await ReferrerAsync();
        using var anonymous = fixture.CreateClient();

        var valid = await anonymous.GetAsync($"/referrals/{code.ToLowerInvariant()}", Token);
        var body = await valid.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.Equal("no-store", valid.Headers.CacheControl?.ToString());
        Assert.True(body.GetProperty("valid").GetBoolean());
        Assert.Equal(new[] { "valid" }, body.EnumerateObject().Select(property => property.Name).ToArray());

        foreach (var unknown in new[] { "ZZZZZZZZ", "0000", "not-a-code-at-all" })
        {
            var refused = await anonymous.GetFromJsonAsync<JsonElement>($"/referrals/{unknown}", Token);
            Assert.False(refused.GetProperty("valid").GetBoolean());
            Assert.Equal(new[] { "valid" }, refused.EnumerateObject().Select(property => property.Name).ToArray());
        }
    }

    [Fact]
    public async Task While_the_programme_is_off_every_code_is_invalid_and_a_sign_up_records_nothing()
    {
        var (clinic, code) = await ReferrerAsync();

        try
        {
            await ConfigureAsync(fixture, enabled: false, cancellationToken: Token);

            using var anonymous = fixture.CreateClient();
            Assert.False((await anonymous.GetFromJsonAsync<JsonElement>($"/referrals/{code}", Token)).GetProperty("valid").GetBoolean());
            Assert.False((await MyReferralAsync(fixture, clinic, Token)).GetProperty("enabled").GetBoolean());

            await BookAsGuestAsync(fixture, clinic, NewPhone(), code, 5, Token);

            Assert.Empty(await StoredForReferrerAsync(clinic.CustomerId));
        }
        finally
        {
            await ConfigureAsync(fixture, cancellationToken: Token);
        }
    }

    [Fact]
    public async Task A_newcomer_booking_with_a_code_is_pending_and_the_answer_says_nothing_about_it()
    {
        var (clinic, code) = await ReferrerAsync();

        var withCode = await BookAsGuestAsync(fixture, clinic, NewPhone(), code, 5, Token);
        var withoutCode = await BookAsGuestAsync(fixture, clinic, NewPhone(), null, 6, Token);
        var friendId = CustomerIdOf(withCode);

        // The friend's answer has the same fields as anyone's: no referrer, no referral.
        Assert.Equal(
            withoutCode.EnumerateObject().Select(property => property.Name).Order(),
            withCode.EnumerateObject().Select(property => property.Name).Order());
        Assert.DoesNotContain("referr", withCode.GetRawText(), StringComparison.OrdinalIgnoreCase);

        using var staff = fixture.CrmDesk(clinic);
        var rows = await ReferralsOfAsync(staff, friendId, Token);
        var row = Assert.Single(rows);

        Assert.Equal("pending", row.GetProperty("status").GetString());
        Assert.Equal(clinic.CustomerId, row.GetProperty("referrer").GetProperty("customer_id").GetString());
        Assert.Equal("คุณลูกค้า ทดสอบ", row.GetProperty("referrer").GetProperty("name").GetString());
        Assert.Equal(friendId, row.GetProperty("friend").GetProperty("customer_id").GetString());
        Assert.Equal("guest_booking", row.GetProperty("channel").GetString());
        Assert.Equal(code, row.GetProperty("code").GetString());

        var mine = await MyReferralAsync(fixture, clinic, Token);
        Assert.Equal(1, mine.GetProperty("invited").GetInt32());
        Assert.Equal(1, mine.GetProperty("pending").GetInt32());
        Assert.DoesNotContain("คุณเพื่อน", mine.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_first_paid_bill_rewards_both_once()
    {
        var (clinic, code) = await ReferrerAsync();
        var friendId = CustomerIdOf(await BookAsGuestAsync(fixture, clinic, NewPhone(), code, 5, Token));
        using var desk = fixture.CrmDesk(clinic);

        var invoiceId = await PayBillAsync(desk, clinic, friendId, Token);
        await PayBillAsync(desk, clinic, friendId, Token);
        await PayBillAsync(desk, clinic, friendId, Token);

        var referrerRows = await ReferralPointsAsync(desk, clinic.CustomerId, Token);
        var friendRows = await ReferralPointsAsync(desk, friendId, Token);
        Assert.Equal(100, Assert.Single(referrerRows).GetProperty("points").GetInt32());
        Assert.Equal(100, Assert.Single(friendRows).GetProperty("points").GetInt32());

        // Referral points do not show the other person's bill in either history.
        Assert.False(Assert.Single(referrerRows).TryGetProperty("invoice_id", out _));

        var row = Assert.Single(await ReferralsOfAsync(desk, friendId, Token));
        Assert.Equal("rewarded", row.GetProperty("status").GetString());
        Assert.Equal(invoiceId, row.GetProperty("reward_invoice_id").GetString());
        Assert.Equal(100, row.GetProperty("referrer_points").GetInt32());
        Assert.Equal(100, row.GetProperty("friend_points").GetInt32());
        Assert.False(row.GetProperty("over_monthly_cap").GetBoolean());

        // The level is judged on money paid: referral points move the balance, not lifetime points.
        var referrer = await CrmScenario.LoyaltyAsync(desk, clinic.CustomerId, Token);
        Assert.Equal(100, referrer.GetProperty("points_balance").GetInt32());
        Assert.Equal(0, referrer.GetProperty("lifetime_points").GetInt32());

        var mine = await MyReferralAsync(fixture, clinic, Token);
        Assert.Equal(1, mine.GetProperty("rewarded").GetInt32());
        Assert.Equal(0, mine.GetProperty("pending").GetInt32());
        Assert.Equal(100, mine.GetProperty("points_earned").GetInt32());
        Assert.Equal(1, mine.GetProperty("rewarded_this_month").GetInt32());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.Equal(1, await db.Set<AuditEntry>().CountAsync(
            entry => entry.Action == AuditActions.ReferralRewarded && entry.ResourceId == row.GetProperty("id").GetString(), Token));
    }

    [Fact]
    public async Task Two_bills_paid_at_the_same_moment_reward_once()
    {
        var (clinic, code) = await ReferrerAsync();
        var friendId = CustomerIdOf(await BookAsGuestAsync(fixture, clinic, NewPhone(), code, 5, Token));
        using var desk = fixture.CrmDesk(clinic);

        await Task.WhenAll(
            PayBillAsync(desk, clinic, friendId, Token),
            PayBillAsync(desk, clinic, friendId, Token),
            PayBillAsync(desk, clinic, friendId, Token));

        Assert.Single(await ReferralPointsAsync(desk, clinic.CustomerId, Token));
        Assert.Single(await ReferralPointsAsync(desk, friendId, Token));
        Assert.Equal("rewarded", Assert.Single(await ReferralsOfAsync(desk, friendId, Token)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Past_the_monthly_cap_the_friend_is_still_rewarded_and_the_referrer_gets_nothing()
    {
        var (clinic, code) = await ReferrerAsync();

        try
        {
            await ConfigureAsync(fixture, monthlyCap: 1, cancellationToken: Token);
            using var desk = fixture.CrmDesk(clinic);

            var first = CustomerIdOf(await BookAsGuestAsync(fixture, clinic, NewPhone(), code, 5, Token));
            var second = CustomerIdOf(await BookAsGuestAsync(fixture, clinic, NewPhone(), code, 6, Token));
            await PayBillAsync(desk, clinic, first, Token);
            await PayBillAsync(desk, clinic, second, Token);

            Assert.Equal(100, Assert.Single(await ReferralPointsAsync(desk, clinic.CustomerId, Token)).GetProperty("points").GetInt32());
            Assert.Equal(100, Assert.Single(await ReferralPointsAsync(desk, second, Token)).GetProperty("points").GetInt32());

            var capped = Assert.Single(await ReferralsOfAsync(desk, second, Token));
            Assert.Equal("rewarded", capped.GetProperty("status").GetString());
            Assert.Equal(0, capped.GetProperty("referrer_points").GetInt32());
            Assert.True(capped.GetProperty("over_monthly_cap").GetBoolean());

            var mine = await MyReferralAsync(fixture, clinic, Token);
            Assert.Equal(2, mine.GetProperty("rewarded").GetInt32());
            Assert.Equal(1, mine.GetProperty("rewarded_this_month").GetInt32());
            Assert.Equal(100, mine.GetProperty("points_earned").GetInt32());
        }
        finally
        {
            await ConfigureAsync(fixture, cancellationToken: Token);
        }
    }

    [Fact]
    public async Task A_customer_using_their_own_code_is_rejected_and_earns_nothing()
    {
        var (clinic, code) = await ReferrerAsync();
        string phone;

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            phone = await db.Set<Customer>().Where(customer => customer.Id == clinic.CustomerId).Select(customer => customer.Phone).SingleAsync(Token);
        }

        var verified = await BookAsGuestAsync(fixture, clinic, phone, code, 5, Token);
        Assert.Equal(clinic.CustomerId, CustomerIdOf(verified));

        using var desk = fixture.CrmDesk(clinic);
        await PayBillAsync(desk, clinic, clinic.CustomerId, Token);

        var row = Assert.Single(await StoredForReferrerAsync(clinic.CustomerId));
        Assert.Equal(ReferralStatus.Rejected, row.Status);
        Assert.Equal(ReferralRejectReasons.SelfReferral, row.RejectReason);
        Assert.Empty(await ReferralPointsAsync(desk, clinic.CustomerId, Token));
    }

    [Fact]
    public async Task A_returning_customer_is_not_a_new_friend()
    {
        var (clinic, code) = await ReferrerAsync();
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        string phone;

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            phone = await db.Set<Customer>().Where(customer => customer.Id == other.CustomerId).Select(customer => customer.Phone).SingleAsync(Token);
        }

        await BookAsGuestAsync(fixture, other, phone, code, 5, Token);

        var row = Assert.Single(await StoredForReferrerAsync(clinic.CustomerId));
        Assert.Equal(other.CustomerId, row.FriendCustomerId);
        Assert.Equal(ReferralRejectReasons.NotNewCustomer, row.RejectReason);
    }

    [Fact]
    public async Task Signing_up_with_the_referrers_email_is_rejected_as_a_shared_contact()
    {
        await ConfigureAsync(fixture, cancellationToken: Token);
        var email = $"ref-{Guid.NewGuid():N}@example.test";

        // A referrer the counter has an email for.
        var referrer = Customer.Create("คุณชวน มีอีเมล", NewPhone(), email);
        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            db.Set<Customer>().Add(referrer);
            await db.SaveChangesAsync(Token);
        }

        using var referrerClient = fixture.Authenticated(TestTokens.Customer(fixture, "usr_referrer_mail", referrer.Id));
        var code = (await referrerClient.GetFromJsonAsync<JsonElement>("/portal/referral", Token)).GetProperty("code").GetString();

        using var client = fixture.CreateClient();
        var phone = NewPhone();
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/auth/otp/request", new { phone, purpose = "register" }, Token)).StatusCode);

        var registered = await client.PostAsJsonAsync("/auth/register", new
        {
            phone,
            code = fixture.SignIn.CodeFor(phone),
            display_name = "คุณเพื่อน อีเมลซ้ำ",
            email = email.ToUpperInvariant(),
            referral_code = code,
        }, Token);
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var row = Assert.Single(await StoredForReferrerAsync(referrer.Id));
        Assert.Equal(ReferralStatus.Rejected, row.Status);
        Assert.Equal(ReferralRejectReasons.SharedContact, row.RejectReason);
        Assert.Equal(ReferralChannels.Register, row.Channel);
    }

    [Fact]
    public async Task A_friend_is_referred_once_and_an_unknown_code_is_ignored()
    {
        var (clinic, code) = await ReferrerAsync();
        var phone = NewPhone();

        var verified = await BookAsGuestAsync(fixture, clinic, phone, "ZZZZZZZZ", 5, Token);
        var friendId = CustomerIdOf(verified);

        using var desk = fixture.CrmDesk(clinic);
        Assert.Empty(await ReferralsOfAsync(desk, friendId, Token));

        // Coming back with a real code later does not make a returning customer a referral.
        await BookAsGuestAsync(fixture, clinic, phone, code, 6, Token);
        var late = Assert.Single(await ReferralsOfAsync(desk, friendId, Token));
        Assert.Equal("not_new_customer", late.GetProperty("reject_reason").GetString());

        await BookAsGuestAsync(fixture, clinic, phone, code, 7, Token);
        Assert.Single(await ReferralsOfAsync(desk, friendId, Token));
    }

    [Fact]
    public async Task The_staff_list_needs_crm_read()
    {
        var (clinic, _) = await ReferrerAsync();
        using var reader = fixture.CrmDesk(clinic, Scopes.Crm.Read);
        using var cashier = fixture.CrmDesk(clinic, Scopes.Invoice.Write);
        using var customer = fixture.CustomerOf(clinic);
        using var anonymous = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/staff/referrals", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/staff/referrals", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/staff/referrals", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/referrals", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/portal/referral", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/portal/referral", Token)).StatusCode);
    }

    [Fact]
    public async Task The_staff_list_pages_newest_first()
    {
        var (clinic, code) = await ReferrerAsync();
        var first = CustomerIdOf(await BookAsGuestAsync(fixture, clinic, NewPhone(), code, 5, Token));
        var second = CustomerIdOf(await BookAsGuestAsync(fixture, clinic, NewPhone(), code, 6, Token));
        using var reader = fixture.CrmDesk(clinic, Scopes.Crm.Read);

        var page = await reader.GetFromJsonAsync<JsonElement>($"/staff/referrals?customer={clinic.CustomerId}&limit=1", Token);
        Assert.Equal(second, page.GetProperty("data")[0].GetProperty("friend").GetProperty("customer_id").GetString());

        var cursor = page.GetProperty("page").GetProperty("next_cursor").GetString();
        var next = await reader.GetFromJsonAsync<JsonElement>($"/staff/referrals?customer={clinic.CustomerId}&limit=1&cursor={Uri.EscapeDataString(cursor!)}", Token);
        Assert.Equal(first, next.GetProperty("data")[0].GetProperty("friend").GetProperty("customer_id").GetString());
    }

    [Fact]
    public async Task The_referral_settings_are_optional_on_the_loyalty_settings_put()
    {
        await ConfigureAsync(fixture, referrerPoints: 150, friendPoints: 80, monthlyCap: 3, cancellationToken: Token);
        using var admin = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Settings.Read, Scopes.Settings.Write]));

        try
        {
            // A client that predates the referral fields leaves them as they are.
            var current = await admin.GetFromJsonAsync<JsonElement>("/clinic/settings/loyalty", Token);
            using var older = new HttpRequestMessage(HttpMethod.Put, "/clinic/settings/loyalty")
            {
                Content = JsonContent.Create(new { satang_per_point = 10_000, point_value_satang = 100, silver_min_points = 200, gold_min_points = 1_000 }),
            };
            older.Headers.TryAddWithoutValidation("If-Match", $"\"{current.GetProperty("version").GetUInt32()}\"");
            var kept = await (await admin.SendAsync(older, Token)).Content.ReadFromJsonAsync<JsonElement>(Token);

            Assert.True(kept.GetProperty("referral_enabled").GetBoolean());
            Assert.Equal(150, kept.GetProperty("referral_referrer_points").GetInt32());
            Assert.Equal(80, kept.GetProperty("referral_friend_points").GetInt32());
            Assert.Equal(3, kept.GetProperty("referral_monthly_cap").GetInt32());

            var bad = await admin.PutAsJsonAsync("/clinic/settings/loyalty", new
            {
                satang_per_point = 10_000,
                point_value_satang = 100,
                silver_min_points = 200,
                gold_min_points = 1_000,
                referral_monthly_cap = 0,
            }, Token);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);
            Assert.Equal("referral_monthly_cap", (await bad.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("field").GetString());

            using var scope = fixture.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var audit = await db.Set<AuditEntry>().AsNoTracking()
                .Where(entry => entry.Action == AuditActions.LoyaltySettingsChanged)
                .OrderByDescending(entry => entry.OccurredAt)
                .FirstAsync(Token);
            Assert.Contains("referral_monthly_cap", audit.DetailsJson ?? string.Empty, StringComparison.Ordinal);
        }
        finally
        {
            await ConfigureAsync(fixture, cancellationToken: Token);
        }
    }

    [Fact]
    public async Task Saving_zero_points_keeps_zero()
    {
        try
        {
            var saved = await ConfigureAsync(fixture, referrerPoints: 0, friendPoints: 0, monthlyCap: 1, cancellationToken: Token);

            Assert.Equal(0, saved.GetProperty("referral_referrer_points").GetInt32());
            Assert.Equal(0, saved.GetProperty("referral_friend_points").GetInt32());
        }
        finally
        {
            await ConfigureAsync(fixture, cancellationToken: Token);
        }
    }
}
