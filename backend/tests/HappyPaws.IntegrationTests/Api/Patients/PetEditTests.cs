using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>
/// <c>PATCH /staff/pets/{id}</c> and the three new fields on the identity card: sex, birth date and
/// neuter status. Not in the API reference; see <c>docs/contract-extensions.md § 28</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class PetEditTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] EditScopes = [Scopes.Patient.Read, Scopes.Patient.Write, Scopes.Audit.Read];

    private HttpClient Counter(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? EditScopes : scopes,
            [clinic.BranchId]));

    private static async Task<HttpResponseMessage> PatchAsync(HttpClient client, string petId, object body, uint? ifMatch)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/staff/pets/{petId}") { Content = JsonContent.Create(body) };

        if (ifMatch is { } version)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        }

        return await client.SendAsync(request, Token);
    }

    private static async Task<JsonElement> PetAsync(HttpClient client, string petId) =>
        (await client.GetFromJsonAsync<JsonElement>($"/staff/pets/{petId}", Token)).GetProperty("pet");

    private static async Task<uint> VersionAsync(HttpClient client, string petId) =>
        (await PetAsync(client, petId)).GetProperty("version").GetUInt32();

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(Token);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>Null fields are left out of the answer, so absent and null read the same.</summary>
    private static bool IsNull(JsonElement element, string name) =>
        !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null;

    private static async Task<JsonElement> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    private static string Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, AutomationScenario.Bangkok).DateTime).ToString("yyyy-MM-dd");

    [Fact]
    public async Task A_new_patient_reads_as_unknown_sex_with_no_birth_date_and_unknown_neuter_status()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var pet = await PetAsync(counter, clinic.PetId);

        Assert.Equal("unknown", pet.GetProperty("sex").GetString());
        Assert.True(IsNull(pet, "birth_date"));
        Assert.False(pet.GetProperty("birth_date_estimated").GetBoolean());
        Assert.True(IsNull(pet, "neutered"));
    }

    [Fact]
    public async Task Staff_correct_the_card_and_the_new_fields_come_back_on_every_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        var version = await VersionAsync(counter, clinic.PetId);

        var response = await PatchAsync(
            counter,
            clinic.PetId,
            new { breed = "พุดเดิ้ล", sex = "female", birth_date = "2021-04-01", birth_date_estimated = true, neutered = true },
            version);

        var pet = await OkAsync(response);

        Assert.Equal("พุดเดิ้ล", pet.GetProperty("breed").GetString());
        Assert.Equal("female", pet.GetProperty("sex").GetString());
        Assert.Equal("2021-04-01", pet.GetProperty("birth_date").GetString());
        Assert.True(pet.GetProperty("birth_date_estimated").GetBoolean());
        Assert.True(pet.GetProperty("neutered").GetBoolean());
        Assert.NotEqual(version, pet.GetProperty("version").GetUInt32());
        Assert.Equal($"\"{pet.GetProperty("version").GetUInt32()}\"", response.Headers.ETag?.ToString());

        var read = await PetAsync(counter, clinic.PetId);
        Assert.Equal("female", read.GetProperty("sex").GetString());
        Assert.Equal("2021-04-01", read.GetProperty("birth_date").GetString());

        // The register lists them too.
        var list = await counter.GetFromJsonAsync<JsonElement>($"/staff/pets?customer_id={clinic.CustomerId}", Token);
        var listed = list.GetProperty("data").EnumerateArray().Single(row => row.GetProperty("id").GetString() == clinic.PetId);
        Assert.True(listed.GetProperty("neutered").GetBoolean());

        // And the owner sees them, read-only, in the portal.
        using var owner = fixture.CustomerOf(clinic);
        var mine = await owner.GetFromJsonAsync<JsonElement>("/portal/pets", Token);
        var card = mine.GetProperty("data").EnumerateArray().Single(row => row.GetProperty("id").GetString() == clinic.PetId);
        Assert.Equal("female", card.GetProperty("sex").GetString());
        Assert.Equal("2021-04-01", card.GetProperty("birth_date").GetString());
    }

    [Fact]
    public async Task Only_what_is_sent_changes_and_the_owners_fields_are_out_of_reach()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);
        (await owner.PatchAsJsonAsync($"/portal/pets/{clinic.PetId}", new { nickname = "ลูลู่" }, Token)).EnsureSuccessStatusCode();

        using var counter = Counter(clinic);
        var pet = await OkAsync(await PatchAsync(
            counter,
            clinic.PetId,
            new { sex = "male", nickname = "ชื่อที่พนักงานตั้งเอง", diet = "อาหารเม็ด" },
            await VersionAsync(counter, clinic.PetId)));

        Assert.Equal("male", pet.GetProperty("sex").GetString());
        Assert.Equal("ลูน่า", pet.GetProperty("name").GetString());
        Assert.Equal("ลูลู่", pet.GetProperty("nickname").GetString());
        Assert.True(IsNull(pet, "diet"));
    }

    [Fact]
    public async Task Birth_date_and_neuter_status_go_back_to_unknown_through_clear()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        await OkAsync(await PatchAsync(
            counter, clinic.PetId, new { birth_date = "2020-01-01", birth_date_estimated = true, neutered = false }, await VersionAsync(counter, clinic.PetId)));

        var pet = await OkAsync(await PatchAsync(
            counter, clinic.PetId, new { clear = new[] { "birth_date", "neutered" } }, await VersionAsync(counter, clinic.PetId)));

        Assert.True(IsNull(pet, "birth_date"));
        Assert.False(pet.GetProperty("birth_date_estimated").GetBoolean());
        Assert.True(IsNull(pet, "neutered"));
    }

    [Fact]
    public async Task It_takes_patient_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = Counter(clinic, Scopes.Patient.Read);

        var response = await PatchAsync(reader, clinic.PetId, new { sex = "male" }, await VersionAsync(reader, clinic.PetId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task If_match_is_required_and_a_stale_version_is_a_conflict()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        var version = await VersionAsync(counter, clinic.PetId);

        var blind = await PatchAsync(counter, clinic.PetId, new { sex = "male" }, ifMatch: null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blind.StatusCode);
        Assert.Equal("If-Match", (await ErrorAsync(blind)).GetProperty("field").GetString());

        await OkAsync(await PatchAsync(counter, clinic.PetId, new { sex = "male" }, version));

        var stale = await PatchAsync(counter, clinic.PetId, new { sex = "female" }, version);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("version_conflict", (await ErrorAsync(stale)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_chip_already_on_another_patient_is_refused_on_its_field_and_its_own_chip_is_not_a_clash()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        var chip = $"900{Random.Shared.Next(100_000_000, 999_999_999)}";

        (await counter.PostAsJsonAsync(
            "/staff/pets",
            new { name = "ตัวที่มีชิปแล้ว", species = "แมว", primary_customer_id = clinic.CustomerId, microchip_number = chip },
            Token)).EnsureSuccessStatusCode();

        var clash = await PatchAsync(counter, clinic.PetId, new { microchip_number = chip }, await VersionAsync(counter, clinic.PetId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, clash.StatusCode);
        Assert.Equal("microchip_number", (await ErrorAsync(clash)).GetProperty("field").GetString());

        var own = $"901{Random.Shared.Next(100_000_000, 999_999_999)}";
        await OkAsync(await PatchAsync(counter, clinic.PetId, new { microchip_number = own }, await VersionAsync(counter, clinic.PetId)));
        var again = await OkAsync(await PatchAsync(counter, clinic.PetId, new { microchip_number = own, sex = "male" }, await VersionAsync(counter, clinic.PetId)));

        Assert.Equal(own, again.GetProperty("microchip_number").GetString());
    }

    [Theory]
    [InlineData("birth_date")]
    [InlineData("birth_date_estimated")]
    [InlineData("sex")]
    [InlineData("clear")]
    [InlineData("name")]
    public async Task A_bad_value_is_refused_on_its_field(string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        object body = field switch
        {
            "birth_date" => new { birth_date = DateOnly.Parse(Today, System.Globalization.CultureInfo.InvariantCulture).AddDays(1).ToString("yyyy-MM-dd") },
            "birth_date_estimated" => new { birth_date_estimated = true },
            "sex" => new { sex = "hermaphrodite" },
            "clear" => new { clear = new[] { "name" } },
            _ => new { name = "  " },
        };

        var response = await PatchAsync(counter, clinic.PetId, body, await VersionAsync(counter, clinic.PetId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(field, (await ErrorAsync(response)).GetProperty("field").GetString());
    }

    [Fact]
    public async Task Today_is_a_valid_birth_date()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var pet = await OkAsync(await PatchAsync(counter, clinic.PetId, new { birth_date = Today }, await VersionAsync(counter, clinic.PetId)));

        Assert.Equal(Today, pet.GetProperty("birth_date").GetString());
    }

    [Fact]
    public async Task A_closed_record_takes_identity_corrections_but_not_a_new_neuter_status()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        (await counter.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/deceased", new { }, Token)).EnsureSuccessStatusCode();

        var corrected = await OkAsync(await PatchAsync(
            counter, clinic.PetId, new { breed = "บางแก้ว", sex = "female", birth_date = "2012-06-01" }, await VersionAsync(counter, clinic.PetId)));
        Assert.Equal("บางแก้ว", corrected.GetProperty("breed").GetString());
        Assert.True(corrected.GetProperty("is_deceased").GetBoolean());

        var neutered = await PatchAsync(counter, clinic.PetId, new { neutered = true }, await VersionAsync(counter, clinic.PetId));
        Assert.Equal(HttpStatusCode.Conflict, neutered.StatusCode);
        Assert.Equal("state_conflict", (await ErrorAsync(neutered)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Each_change_is_audited_with_before_and_after_and_a_request_that_changes_nothing_writes_nothing()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        await OkAsync(await PatchAsync(counter, clinic.PetId, new { sex = "female", birth_date = "2022-02-02" }, await VersionAsync(counter, clinic.PetId)));
        await OkAsync(await PatchAsync(counter, clinic.PetId, new { sex = "female" }, await VersionAsync(counter, clinic.PetId)));

        var log = await counter.GetFromJsonAsync<JsonElement>($"/staff/audit-logs?pet_id={clinic.PetId}", Token);
        var updates = log.GetProperty("data").EnumerateArray()
            .Where(row => row.GetProperty("action").GetString() == "pet.updated")
            .ToList();

        var update = Assert.Single(updates);
        var changes = update.GetProperty("details").GetProperty("changes");

        Assert.Equal("unknown", changes.GetProperty("sex").GetProperty("before").GetString());
        Assert.Equal("female", changes.GetProperty("sex").GetProperty("after").GetString());
        Assert.Equal("2022-02-02", changes.GetProperty("birth_date").GetProperty("after").GetString());
        Assert.False(changes.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task Registering_a_patient_takes_the_new_fields_and_refuses_a_birth_date_in_the_future()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var created = await OkAsync(await counter.PostAsJsonAsync(
            "/staff/pets",
            new
            {
                name = "ถั่วเหลือง",
                species = "แมว",
                primary_customer_id = clinic.CustomerId,
                sex = "male",
                birth_date = "2024-12-01",
                birth_date_estimated = true,
                neutered = false,
            },
            Token));

        Assert.Equal("male", created.GetProperty("sex").GetString());
        Assert.Equal("2024-12-01", created.GetProperty("birth_date").GetString());
        Assert.True(created.GetProperty("birth_date_estimated").GetBoolean());
        Assert.False(created.GetProperty("neutered").GetBoolean());

        var future = await counter.PostAsJsonAsync(
            "/staff/pets",
            new { name = "ยังไม่เกิด", species = "แมว", primary_customer_id = clinic.CustomerId, birth_date = "2999-01-01" },
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, future.StatusCode);
        Assert.Equal("birth_date", (await ErrorAsync(future)).GetProperty("field").GetString());
    }
}
