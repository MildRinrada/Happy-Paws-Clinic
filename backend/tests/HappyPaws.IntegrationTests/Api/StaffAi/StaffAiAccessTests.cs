using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.StaffAi;

/// <summary>
/// Who may use the staff AI assistant (<c>docs/contract-extensions.md § 34</c>): every model route
/// needs <c>ai:use</c> and the scope of the data it reads, the brief needs only <c>dashboard:read</c>.
/// The scope check runs before anything else, so these use ids that do not exist.
/// </summary>
[Collection(nameof(ApiCollection))]
public class StaffAiAccessTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed record Route(HttpMethod Method, string Path, object? Body, string[] DataScopes, bool NeedsAi);

    private static readonly Route[] Routes =
    [
        new(HttpMethod.Post, "/staff/ai/pets/pet_missing/summary", null, [Scopes.Record.Read], true),
        new(HttpMethod.Post, "/staff/ai/consultations/con_missing/soap-draft", new { notes = "อาเจียน" }, [Scopes.Consult.Write], true),
        new(HttpMethod.Post, "/staff/ai/drafts/owner-message", new { kind = "thread_reply", thread_id = "thr_missing" }, [Scopes.Message.Write], true),
        new(HttpMethod.Post, "/staff/ai/ask", new { messages = new[] { new { role = "user", text = "วันนี้มีนัดกี่ราย" } } }, [], true),
        new(HttpMethod.Get, "/staff/ai/daily-brief?branch_id=brn_missing", null, [Scopes.Dashboard.Read], false),
        new(HttpMethod.Get, "/staff/ai/usage", null, [], true),
    ];

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, Route route)
    {
        using var request = new HttpRequestMessage(route.Method, route.Path);
        if (route.Body is not null)
        {
            request.Content = JsonContent.Create(route.Body);
        }

        return await client.SendAsync(request, Token);
    }

    private static async Task<JsonElement> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    [Fact]
    public async Task Every_route_needs_a_session()
    {
        using var anyone = fixture.CreateClient();

        foreach (var route in Routes)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(anyone, route)).StatusCode);
        }
    }

    [Fact]
    public async Task A_role_without_ai_use_is_refused_with_the_missing_scope_named()
    {
        // Every data scope there is, and not ai:use: the role an admin took the assistant away from.
        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture,
            scopes: [Scopes.Record.Read, Scopes.Consult.Write, Scopes.Consult.Read, Scopes.Message.Write, Scopes.Message.Read]));

        foreach (var route in Routes.Where(route => route.NeedsAi))
        {
            var response = await SendAsync(staff, route);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var error = await ErrorAsync(response);
            Assert.Equal("insufficient_scope", error.GetProperty("code").GetString());
            Assert.Contains(Scopes.Ai.Use, error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(item => item.GetString()));
        }
    }

    [Fact]
    public async Task Ai_use_alone_does_not_reach_data_the_caller_could_not_read()
    {
        using var staff = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Ai.Use]));

        foreach (var route in Routes.Where(route => route.DataScopes.Length > 0))
        {
            var response = await SendAsync(staff, route);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Contains(
                route.DataScopes[0],
                (await ErrorAsync(response)).GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(item => item.GetString()));
        }
    }

    [Fact]
    public async Task A_customer_cannot_use_the_staff_assistant()
    {
        using var customer = fixture.Authenticated(TestTokens.Customer(fixture));

        foreach (var route in Routes)
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(customer, route)).StatusCode);
        }
    }

    [Fact]
    public async Task An_aftercare_draft_also_needs_the_right_to_read_the_case()
    {
        // message:write and ai:use pass the route; the case's diagnoses and medicines need consult:read.
        using var staff = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Message.Write, Scopes.Ai.Use]));

        var response = await staff.PostAsJsonAsync("/staff/ai/drafts/owner-message", new { kind = "aftercare", consultation_id = "con_missing" }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            Scopes.Consult.Read,
            (await ErrorAsync(response)).GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task Ai_use_is_in_the_catalogue_a_role_can_be_given_but_never_on_an_api_key()
    {
        using var admin = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Role.Read]));
        var catalogue = await admin.GetStringAsync("/staff/permissions", Token);

        Assert.Contains("\"scope\":\"ai:use\"", catalogue);
        Assert.True(ScopeCatalogue.IsGrantable(Scopes.Ai.Use));
        Assert.False(ApiKeyScopeRules.MayCarry(Scopes.Ai.Use));
    }
}
