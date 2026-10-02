using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>
/// Who may reach a patient, and what happens when two customer records turn out to be one person.
/// <para>
/// The carer tests matter more than they look: these two routes are the access-control surface of
/// the entire portal.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class CarerAndMergeTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] CounterScopes =
    [
        Scopes.Customer.Read, Scopes.Customer.Write, Scopes.Customer.Merge,
        Scopes.Patient.Read, Scopes.Patient.Write, Scopes.Record.Read,
    ];

    private HttpClient Counter(SeededClinic clinic) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, CounterScopes, [clinic.BranchId]));

    private async Task<string> CustomerAsync(HttpClient counter, string name)
    {
        var response = await counter.PostAsJsonAsync(
            "/staff/customers",
            new { display_name = name, phone = $"08{Random.Shared.Next(10_000_000, 99_999_999)}" },
            Token);

        response.EnsureSuccessStatusCode();

        var customer = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        return customer.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task A_co_owner_gains_and_then_loses_access_without_losing_the_record_of_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var relative = await CustomerAsync(counter, "คุณป้า");

        var added = await counter.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/owners",
            new { customer_id = relative, may_view_history = false },
            Token);

        Assert.Equal(HttpStatusCode.Created, added.StatusCode);

        var carer = await added.Content.ReadFromJsonAsync<JsonElement>(Token);

        // "กำหนดสิทธิ์ดูประวัติ" — a relative can bring the animal in without reading its file.
        Assert.False(carer.GetProperty("may_view_history").GetBoolean());
        Assert.False(carer.GetProperty("is_primary").GetBoolean());

        var removed = await counter.DeleteAsync(
            $"/staff/pets/{clinic.PetId}/owners/{relative}", Token);

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var detail = await counter.GetFromJsonAsync<JsonElement>($"/staff/pets/{clinic.PetId}", Token);
        var closed = detail.GetProperty("carers").EnumerateArray()
            .Single(link => link.GetProperty("customer_id").GetString() == relative);

        // "ถอนสิทธิ์เข้าถึง ไม่ลบประวัติ" — the link is closed, not erased.
        Assert.True(closed.TryGetProperty("revoked_at", out _));
    }

    [Fact]
    public async Task Re_adding_a_carer_updates_their_access_instead_of_failing()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var relative = await CustomerAsync(counter, "คุณลุง");

        (await counter.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/owners",
            new { customer_id = relative, may_view_history = false },
            Token)).EnsureSuccessStatusCode();

        var again = await counter.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/owners",
            new { customer_id = relative, may_view_history = true },
            Token);

        again.EnsureSuccessStatusCode();

        var carer = await again.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.True(carer.GetProperty("may_view_history").GetBoolean());
    }

    [Fact]
    public async Task The_primary_carer_cannot_be_removed()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var response = await counter.DeleteAsync(
            $"/staff/pets/{clinic.PetId}/owners/{clinic.CustomerId}", Token);

        // Removing the last carer would orphan the record and everything that hangs off the owner.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_merge_moves_the_patients_and_leaves_a_tombstone_behind()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var duplicate = await CustomerAsync(counter, "สมชาย (ซ้ำ)");

        var pet = await counter.PostAsJsonAsync(
            "/staff/pets",
            new { name = "ข้าวปุ้น", species = "สุนัข", primary_customer_id = duplicate },
            Token);
        pet.EnsureSuccessStatusCode();
        var petId = (await pet.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!;

        var merged = await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/merge",
            new { duplicate_customer_id = duplicate },
            Token);

        merged.EnsureSuccessStatusCode();

        var result = await merged.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(1, result.GetProperty("moved").GetProperty("pets").GetInt32());
        Assert.Equal(
            clinic.CustomerId,
            result.GetProperty("merged").GetProperty("merged_into_customer_id").GetString());

        // The patient followed the person.
        var survivor = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers/{clinic.CustomerId}", Token);

        Assert.Contains(
            survivor.GetProperty("pets").EnumerateArray(),
            owned => owned.GetProperty("id").GetString() == petId);

        // The tombstone still resolves rather than answering 404 — an id on an old receipt should
        // say where it went.
        var stale = await counter.GetAsync($"/staff/customers/{duplicate}", Token);
        stale.EnsureSuccessStatusCode();

        // But it never appears in search again.
        var found = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers?q={Uri.EscapeDataString("สมชาย (ซ้ำ)")}", Token);

        Assert.Empty(found.GetProperty("data").EnumerateArray());
    }

    [Fact]
    public async Task A_merge_folds_a_carer_link_both_records_held()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var duplicate = await CustomerAsync(counter, "สมหญิง (ซ้ำ)");

        // Both records care for the seeded patient — which, once they turn out to be one person,
        // is one person linked to one animal twice.
        (await counter.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/owners",
            new { customer_id = duplicate, may_view_history = true },
            Token)).EnsureSuccessStatusCode();

        var merged = await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/merge",
            new { duplicate_customer_id = duplicate },
            Token);

        // Without folding, the bulk UPDATE would violate the unique index on
        // (pet_id, customer_id) WHERE revoked_at IS NULL — correctly.
        merged.EnsureSuccessStatusCode();

        var result = await merged.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(1, result.GetProperty("moved").GetProperty("carer_links_folded").GetInt32());

        var detail = await counter.GetFromJsonAsync<JsonElement>($"/staff/pets/{clinic.PetId}", Token);
        var live = detail.GetProperty("carers").EnumerateArray()
            .Where(link => !link.TryGetProperty("revoked_at", out _))
            .ToList();

        Assert.Single(live);
        Assert.True(live[0].GetProperty("is_primary").GetBoolean());
    }

    [Fact]
    public async Task A_merge_moves_what_is_owed_onto_the_surviving_record()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        using var cashier = fixture.Cashier(clinic);

        var duplicate = await CustomerAsync(counter, "สมศักดิ์ (ซ้ำ)");

        await BillingScenario.RaiseAsync(cashier, clinic, Token, new
        {
            customer_id = duplicate,
            branch_id = clinic.BranchId,
            lines = new object[]
            {
                new { description = "ค่าฝากเลี้ยง", quantity = 2, unit_price_satang = 30_000 },
            },
        });

        (await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/merge",
            new { duplicate_customer_id = duplicate },
            Token)).EnsureSuccessStatusCode();

        var survivor = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers/{clinic.CustomerId}", Token);

        // A debt that stayed behind on a tombstone is a debt nobody collects.
        Assert.Equal(60_000, survivor.GetProperty("outstanding_satang").GetInt64());
    }

    [Fact]
    public async Task A_record_cannot_be_merged_twice()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var duplicate = await CustomerAsync(counter, "ซ้ำสอง");

        (await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/merge",
            new { duplicate_customer_id = duplicate },
            Token)).EnsureSuccessStatusCode();

        var again = await counter.PostAsJsonAsync(
            $"/staff/customers/{clinic.CustomerId}/merge",
            new { duplicate_customer_id = duplicate },
            Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }
}
