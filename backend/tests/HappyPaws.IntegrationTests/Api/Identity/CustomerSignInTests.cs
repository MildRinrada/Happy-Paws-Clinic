using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Customers;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Identity;

/// <summary>
/// "สมัครด้วยเบอร์โทร + OTP", "rate limit 3 ครั้ง/เบอร์/10 นาที", "คืน capabilities ของบัญชีในคลินิกนี้"
/// and "ลิงก์อายุ 30 นาที ใช้ครั้งเดียว", driven the way a phone would.
/// </summary>
[Collection(nameof(ApiCollection))]
public class CustomerSignInTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string NewPhone() => $"09{Random.Shared.Next(10_000_000, 99_999_999)}";

    private async Task<string> CodeAsync(HttpClient client, string phone, string purpose = "register")
    {
        var response = await client.PostAsJsonAsync("/auth/otp/request", new { phone, purpose }, Token);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        return fixture.SignIn.CodeFor(phone) ?? throw new InvalidOperationException("no code was sent");
    }

    private async Task<JsonElement> RegisterAsync(HttpClient client, string phone, string? password = null)
    {
        var code = await CodeAsync(client, phone);

        var response = await client.PostAsJsonAsync(
            "/auth/register", new { phone, code, display_name = "คุณลูกค้า สมัครเอง", password }, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    [Fact]
    public async Task Signing_up_with_the_number_the_counter_has_links_that_record_and_its_pets()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        string phone;

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            phone = db.Set<Customer>().Single(customer => customer.Id == clinic.CustomerId).Phone;
        }

        using var client = fixture.CreateClient();
        var tokens = await RegisterAsync(client, phone);
        var capabilities = tokens.GetProperty("capabilities");

        Assert.Equal(clinic.CustomerId, capabilities.GetProperty("customer_id").GetString());
        Assert.Contains(capabilities.GetProperty("scopes").EnumerateArray(), scope => scope.GetString() == Scopes.Portal);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("access_token").GetString());
        var pets = await client.GetFromJsonAsync<JsonElement>("/portal/pets", Token);

        Assert.Equal(clinic.PetId, pets.GetProperty("data")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task A_number_with_an_account_cannot_sign_up_again()
    {
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        await RegisterAsync(client, phone);

        var code = await CodeAsync(client, phone);
        var again = await client.PostAsJsonAsync("/auth/register", new { phone, code, display_name = "อีกครั้ง" }, Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task A_code_dies_after_five_wrong_guesses()
    {
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        var code = await CodeAsync(client, phone);
        var wrong = code == "000000" ? "111111" : "000000";

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var guess = await client.PostAsJsonAsync("/auth/register", new { phone, code = wrong, display_name = "x" }, Token);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, guess.StatusCode);
        }

        var right = await client.PostAsJsonAsync("/auth/register", new { phone, code, display_name = "x" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, right.StatusCode);
    }

    [Fact]
    public async Task A_fourth_code_inside_ten_minutes_is_refused()
    {
        var phone = NewPhone();
        using var client = fixture.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            await CodeAsync(client, phone);
        }

        var fourth = await client.PostAsJsonAsync("/auth/otp/request", new { phone, purpose = "register" }, Token);

        Assert.Equal(HttpStatusCode.TooManyRequests, fourth.StatusCode);
    }

    [Fact]
    public async Task A_login_code_for_an_unknown_number_is_answered_the_same_and_never_sent()
    {
        var phone = NewPhone();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/otp/request", new { phone, purpose = "login" }, Token);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(0, fixture.SignIn.CodesSentTo(phone));

        var login = await client.PostAsJsonAsync("/auth/login", new { phone, code = "123456" }, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task A_customer_signs_in_with_a_code_or_with_a_password()
    {
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        await RegisterAsync(client, phone, password: "a-long-enough-password");

        var code = await CodeAsync(client, phone, "login");
        var withCode = await client.PostAsJsonAsync("/auth/login", new { phone, code }, Token);
        var withPassword = await client.PostAsJsonAsync("/auth/login", new { identifier = phone, password = "a-long-enough-password" }, Token);
        var wrongPassword = await client.PostAsJsonAsync("/auth/login", new { identifier = phone, password = "not-the-password" }, Token);

        Assert.Equal(HttpStatusCode.OK, withCode.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
    }

    [Fact]
    public async Task Staff_with_a_second_factor_get_a_challenge_from_the_shared_login()
    {
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Queue.Read], withTwoFactor: true, cancellationToken: Token);
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { identifier = staff.Email, password = staff.Password }, Token);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.True(body.GetProperty("two_factor_required").GetBoolean());
        Assert.False(body.TryGetProperty("access_token", out _));
    }

    [Fact]
    public async Task A_reset_link_sets_a_new_password_once()
    {
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        var registered = await RegisterAsync(client, phone, password: "the-first-password");

        var ask = await client.PostAsJsonAsync("/auth/password/reset", new { identifier = phone }, Token);
        Assert.Equal(HttpStatusCode.Accepted, ask.StatusCode);

        var userId = await UserIdAsync(registered);
        var token = fixture.SignIn.ResetTokenFor(userId)!;

        var reset = await client.PostAsJsonAsync("/auth/password/reset", new { token, new_password = "the-second-password" }, Token);
        var reuse = await client.PostAsJsonAsync("/auth/password/reset", new { token, new_password = "a-third-password" }, Token);

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reuse.StatusCode);

        var oldPassword = await client.PostAsJsonAsync("/auth/login", new { identifier = phone, password = "the-first-password" }, Token);
        var newPassword = await client.PostAsJsonAsync("/auth/login", new { identifier = phone, password = "the-second-password" }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newPassword.StatusCode);
    }

    [Fact]
    public async Task Asking_for_a_reset_for_nobody_looks_the_same()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/password/reset", new { identifier = "nobody@nowhere.test" }, Token);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("link_sent", body.GetProperty("status").GetString());
    }

    private async Task<string> UserIdAsync(JsonElement tokens)
    {
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("access_token").GetString());

        var me = await client.GetFromJsonAsync<JsonElement>("/me", Token);
        return me.GetProperty("user_id").GetString()!;
    }
}
