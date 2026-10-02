using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>
/// The counter's register: find someone, create someone, look them up, invite them.
/// </summary>
[Collection(nameof(ApiCollection))]
public class CustomerRegisterTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] CounterScopes =
    [
        Scopes.Customer.Read, Scopes.Customer.Write, Scopes.Customer.Merge,
        Scopes.Patient.Read, Scopes.Patient.Write,
    ];

    private HttpClient Counter(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? CounterScopes : scopes,
            [clinic.BranchId]));

    [Fact]
    public async Task A_customer_created_at_the_counter_is_findable_by_name_and_by_phone()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var phone = $"08{Random.Shared.Next(10_000_000, 99_999_999)}";
        var name = $"คุณทดสอบ {Guid.NewGuid():N}"[..24];

        var created = await counter.PostAsJsonAsync(
            "/staff/customers", new { display_name = name, phone }, Token);

        created.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var byName = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers?q={Uri.EscapeDataString(name)}", Token);
        var byPhone = await counter.GetFromJsonAsync<JsonElement>($"/staff/customers?q={phone}", Token);

        Assert.Equal(1, byName.GetProperty("data").GetArrayLength());
        Assert.Equal(1, byPhone.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task A_repeated_phone_number_is_refused_once_and_then_allowed()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var phone = $"08{Random.Shared.Next(10_000_000, 99_999_999)}";

        (await counter.PostAsJsonAsync(
            "/staff/customers", new { display_name = "คุณแม่", phone }, Token)).EnsureSuccessStatusCode();

        var refused = await counter.PostAsJsonAsync(
            "/staff/customers", new { display_name = "คุณลูก", phone }, Token);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        var error = await refused.Content.ReadFromJsonAsync<JsonElement>(Token);
        var body = error.GetProperty("error");

        Assert.Equal("duplicate_customer", body.GetProperty("code").GetString());

        // The existing records travel with the refusal so the counter can pick one instead of
        // creating a second — a bare 409 would make the check an obstacle rather than a help.
        Assert.Equal(1, body.GetProperty("details").GetProperty("existing").GetArrayLength());

        // A household really does share a number, so the check is a stop, not a wall.
        var allowed = await counter.PostAsJsonAsync(
            "/staff/customers",
            new { display_name = "คุณลูก", phone, allow_duplicate_phone = true },
            Token);

        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
    }

    [Fact]
    public async Task Searching_by_a_pets_name_finds_its_owner()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        // The seeded patient is named ลูน่า and its owner is the seeded customer.
        var found = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers?q={Uri.EscapeDataString("ลูน่า")}", Token);

        var ids = found.GetProperty("data").EnumerateArray()
            .Select(customer => customer.GetProperty("id").GetString()).ToList();

        // "ค้นด้วย…ชื่อสัตว์" — one box, and the person saying "I'm here with ลูน่า" does not know
        // which table the clinic files that under.
        Assert.Contains(clinic.CustomerId, ids);
    }

    [Fact]
    public async Task A_customer_record_carries_their_pets_and_what_they_owe()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        using var cashier = fixture.Cashier(clinic);

        await BillingScenario.RaiseIdAsync(cashier, clinic, Token);

        var detail = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers/{clinic.CustomerId}", Token);

        Assert.Equal(clinic.CustomerId, detail.GetProperty("customer").GetProperty("id").GetString());

        var pets = detail.GetProperty("pets").EnumerateArray()
            .Select(pet => pet.GetProperty("id").GetString()).ToList();
        Assert.Contains(clinic.PetId, pets);

        // "ยอดค้างชำระ" — computed by Billing, not re-derived here, so it cannot disagree with the
        // invoice list.
        Assert.Equal(146_000, detail.GetProperty("outstanding_satang").GetInt64());
    }

    [Fact]
    public async Task A_settled_bill_stops_counting_against_the_customer()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await BillingScenario.RaiseIdAsync(cashier, clinic, Token);
        (await BillingScenario.PayAsync(cashier, invoiceId, 146_000, Token)).Dispose();

        var detail = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers/{clinic.CustomerId}", Token);

        Assert.Equal(0, detail.GetProperty("outstanding_satang").GetInt64());
    }

    [Fact]
    public async Task Inviting_twice_refreshes_one_invitation_rather_than_stacking_two()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var first = await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/invite", new { }, Token);
        first.EnsureSuccessStatusCode();

        var one = await first.Content.ReadFromJsonAsync<JsonElement>(Token);

        var second = await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/invite", new { }, Token);
        second.EnsureSuccessStatusCode();

        var two = await second.Content.ReadFromJsonAsync<JsonElement>(Token);

        // "Have we asked them, and is that still live" has to stay a single answerable question.
        Assert.Equal(one.GetProperty("id").GetString(), two.GetProperty("id").GetString());
        Assert.True(two.GetProperty("is_live").GetBoolean());

        Assert.Contains(
            fixture.Notifications.SentTo(clinic.CustomerId),
            sent => sent.TemplateKey == "customer.portal_invitation");
    }

    [Theory]
    [InlineData("GET", "/staff/customers", Scopes.Customer.Read)]
    [InlineData("POST", "/staff/customers", Scopes.Customer.Write)]
    [InlineData("GET", "/staff/customers/cus_x", Scopes.Customer.Read)]
    [InlineData("POST", "/staff/customers/cus_x/invite", Scopes.Customer.Write)]
    [InlineData("POST", "/staff/customers/cus_x/merge", Scopes.Customer.Merge)]
    public async Task Every_customer_route_refuses_a_caller_without_its_scope(
        string method,
        string path,
        string required)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var underscoped = Counter(clinic, [.. CounterScopes.Where(scope => scope != required)]);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await underscoped.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
