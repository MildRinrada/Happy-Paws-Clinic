using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>
/// The patient register: register one, look one up, and read its file.
/// </summary>
[Collection(nameof(ApiCollection))]
public class PetRegisterTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] RegistryScopes =
    [
        Scopes.Patient.Read, Scopes.Patient.Write, Scopes.Record.Read, Scopes.Record.Write, Scopes.Audit.Read,
    ];

    private HttpClient Counter(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? RegistryScopes : scopes,
            [clinic.BranchId]));

    [Fact]
    public async Task Registering_a_patient_issues_an_HN_and_links_the_owner_both_ways()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var response = await counter.PostAsJsonAsync(
            "/staff/pets",
            new
            {
                name = "มิโล",
                species = "แมว",
                breed = "วิเชียรมาศ",
                primary_customer_id = clinic.CustomerId,
            },
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var pet = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var petId = pet.GetProperty("id").GetString()!;

        // "ออก HN อัตโนมัติ" — the server issues it; the request never carries one.
        Assert.StartsWith("HN-", pet.GetProperty("hospital_number").GetString(), StringComparison.Ordinal);
        Assert.Equal(clinic.CustomerId, pet.GetProperty("primary_customer_id").GetString());

        // "ผูกเจ้าของหลัก" is written twice, and this is the half that governs access.
        var detail = await counter.GetFromJsonAsync<JsonElement>($"/staff/pets/{petId}", Token);
        var carers = detail.GetProperty("carers");

        Assert.Equal(1, carers.GetArrayLength());
        Assert.True(carers[0].GetProperty("is_primary").GetBoolean());
        Assert.Equal(clinic.CustomerId, carers[0].GetProperty("customer_id").GetString());
    }

    [Fact]
    public async Task Two_patients_registered_together_get_different_hospital_numbers()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var numbers = new List<string>();

        foreach (var name in new[] { "มิโล", "ข้าวปุ้น" })
        {
            var created = await counter.PostAsJsonAsync(
                "/staff/pets",
                new { name, species = "แมว", primary_customer_id = clinic.CustomerId },
                Token);

            created.EnsureSuccessStatusCode();

            var pet = await created.Content.ReadFromJsonAsync<JsonElement>(Token);
            numbers.Add(pet.GetProperty("hospital_number").GetString()!);
        }

        // The whole reason HN allocation takes an advisory lock rather than counting rows.
        Assert.Equal(2, numbers.Distinct().Count());
    }

    [Fact]
    public async Task A_microchip_already_on_another_patient_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var chip = $"900{Random.Shared.Next(100_000_000, 999_999_999)}";
        object Body(string name) => new
        {
            name,
            species = "สุนัข",
            primary_customer_id = clinic.CustomerId,
            microchip_number = chip,
        };

        (await counter.PostAsJsonAsync("/staff/pets", Body("ตัวแรก"), Token)).EnsureSuccessStatusCode();

        var second = await counter.PostAsJsonAsync("/staff/pets", Body("ตัวที่สอง"), Token);

        // A chip identifies exactly one animal in the world; a second patient carrying it is a typo
        // or a patient already registered, and both are worth stopping before a duplicate record
        // exists.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }

    [Fact]
    public async Task The_register_hides_closed_records_until_asked_for_them()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        (await counter.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/deceased", new { }, Token)).EnsureSuccessStatusCode();

        var live = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/pets?customer_id={clinic.CustomerId}", Token);
        var all = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/pets?customer_id={clinic.CustomerId}&include_deceased=true", Token);

        Assert.DoesNotContain(
            live.GetProperty("data").EnumerateArray(),
            pet => pet.GetProperty("id").GetString() == clinic.PetId);

        Assert.Contains(
            all.GetProperty("data").EnumerateArray(),
            pet => pet.GetProperty("id").GetString() == clinic.PetId);
    }

    [Fact]
    public async Task A_patient_record_shows_the_latest_weight_and_the_allergy_banner()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);
        using var counter = Counter(open.Clinic);

        using var vitals = BookingScenario.Idempotent(
            $"/staff/consultations/{open.ConsultationId}/vitals",
            new { weight_kg = 8.4, temperature_c = 39.2 });
        (await vet.SendAsync(vitals, Token)).EnsureSuccessStatusCode();

        (await counter.PostAsJsonAsync(
            $"/staff/pets/{open.Clinic.PetId}/allergies",
            new { substance = "Penicillin", severity = "severe" },
            Token)).EnsureSuccessStatusCode();

        var detail = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/pets/{open.Clinic.PetId}", Token);

        // Read from the vitals rows the weight graph reads, so the card and the chart can never
        // disagree.
        Assert.Equal(8.4m, detail.GetProperty("latest_weight").GetProperty("weight_kg").GetDecimal());

        // The red banner ships with the patient rather than behind a second request.
        Assert.Equal(1, detail.GetProperty("allergies").GetArrayLength());
    }

    [Fact]
    public async Task The_timeline_carries_the_visit_and_the_medicine_and_is_audited()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);
        using var counter = Counter(open.Clinic);

        await DiagnoseAsync(vet, open.ConsultationId, Token);

        using var prescribe = BookingScenario.Idempotent(
            $"/staff/consultations/{open.ConsultationId}/prescriptions",
            new
            {
                drug_name = "Amoxicillin",
                dose = "250mg",
                frequency = "วันละ 2 ครั้ง",
                duration_days = 7,
                quantity = 14,
            });
        (await vet.SendAsync(prescribe, Token)).EnsureSuccessStatusCode();

        await CloseAsync(vet, open.ConsultationId, Token);

        var history = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/pets/{open.Clinic.PetId}/history", Token);

        var kinds = history.GetProperty("data").EnumerateArray()
            .Select(entry => entry.GetProperty("kind").GetString()).ToList();

        Assert.Contains("visit", kinds);
        Assert.Contains("medicine", kinds);

        // pet.history_viewed has been in AuditActions since the registry was written, with nothing
        // calling it. The list was declared as a requirement, not an inventory, so that this
        // endpoint could not arrive without its row.
        var log = await counter.GetFromJsonAsync<JsonElement>("/staff/audit-logs/access", Token);

        Assert.Contains(
            log.GetProperty("data").EnumerateArray(),
            entry => entry.GetProperty("action").GetString() == "pet.history_viewed"
                && entry.GetProperty("resource_id").GetString() == open.Clinic.PetId);
    }

    [Fact]
    public async Task The_timeline_needs_record_read_not_patient_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var registryOnly = Counter(clinic, Scopes.Patient.Read, Scopes.Patient.Write);

        var card = await registryOnly.GetAsync($"/staff/pets/{clinic.PetId}", Token);
        var file = await registryOnly.GetAsync($"/staff/pets/{clinic.PetId}/history", Token);

        // The registry entry is an identity card; the timeline is a medical file. The contract
        // draws that line by giving them different scopes, and this is it holding.
        card.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, file.StatusCode);
    }
}
