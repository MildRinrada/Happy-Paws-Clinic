using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>
/// "รอบที่ฉีดแล้วและรอบถัดไปที่ครบกำหนด", "ล็อตวัคซีน วันหมดอายุ · ตั้งรอบถัดไปอัตโนมัติ" and the weight
/// graph.
/// </summary>
[Collection(nameof(ApiCollection))]
public class VaccinationAndWeightTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] Scopes_ = [Scopes.Patient.Read, Scopes.Record.Write, .. ClinicalScopes];

    private static object Dose(string vaccine = "Rabies", int? validForDays = null, string? nextDueOn = null, string? vialExpiresOn = null) => new
    {
        vaccine_name = vaccine,
        lot_number = "LOT-2026-114",
        vial_expires_on = vialExpiresOn ?? DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
        valid_for_days = validForDays,
        next_due_on = nextDueOn,
    };

    [Fact]
    public async Task A_dose_sets_the_next_one_a_year_on_and_the_card_lists_both()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic, Scopes_);

        var response = await vet.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/vaccinations", Dose(), Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dose = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var today = DateOnly.Parse(ClinicSeed.ClinicDate(DateTimeOffset.UtcNow));
        Assert.Equal(today.AddDays(365).ToString("yyyy-MM-dd"), dose.GetProperty("next_due_on").GetString());

        var card = await vet.GetFromJsonAsync<JsonElement>($"/staff/pets/{clinic.PetId}/vaccinations", Token);
        Assert.Equal(1, card.GetProperty("doses").GetArrayLength());
        Assert.Equal("upcoming", card.GetProperty("due")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Only_the_latest_dose_of_a_vaccine_decides_what_is_due()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic, Scopes_);

        var lastYear = DateTimeOffset.UtcNow.AddDays(-400);
        (await vet.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/vaccinations",
            new { vaccine_name = "DHPPi", lot_number = "OLD-1", vial_expires_on = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"), administered_at = lastYear },
            Token)).EnsureSuccessStatusCode();

        (await vet.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/vaccinations", Dose("dhppi"), Token)).EnsureSuccessStatusCode();

        var card = await vet.GetFromJsonAsync<JsonElement>($"/staff/pets/{clinic.PetId}/vaccinations", Token);

        // Two doses, one due date — the old dose's date was met by the new dose, not overdue.
        Assert.Equal(2, card.GetProperty("doses").GetArrayLength());
        Assert.Equal(1, card.GetProperty("due").GetArrayLength());
        Assert.Equal("upcoming", card.GetProperty("due")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_dose_from_an_expired_vial_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic, Scopes_);

        var response = await vet.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/vaccinations",
            Dose(vialExpiresOn: DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd")),
            Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("invalid_vaccination", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Recording_a_dose_needs_record_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var assistant = fixture.Vet(clinic, Scopes.Patient.Read);

        var response = await assistant.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/vaccinations", Dose(), Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_weight_graph_reads_the_weights_taken_in_visits()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic, Scopes_);

        foreach (var kg in new[] { 4.2m, 4.4m })
        {
            (await vet.PostAsJsonAsync($"/staff/consultations/{open.ConsultationId}/vitals", new { weight_kg = kg }, Token))
                .EnsureSuccessStatusCode();
        }

        var graph = await vet.GetFromJsonAsync<JsonElement>($"/staff/pets/{open.Clinic.PetId}/weights", Token);
        var points = graph.GetProperty("points");

        Assert.Equal("kg", graph.GetProperty("unit").GetString());
        Assert.Equal(2, points.GetArrayLength());
        Assert.Equal(4.2m, points[0].GetProperty("weight_kg").GetDecimal());
        Assert.Equal(4.4m, points[1].GetProperty("weight_kg").GetDecimal());
    }

    [Fact]
    public async Task Vaccinations_appear_on_the_patient_timeline()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic, Scopes_);

        (await vet.PostAsJsonAsync($"/staff/pets/{clinic.PetId}/vaccinations", Dose(), Token)).EnsureSuccessStatusCode();

        var history = await vet.GetFromJsonAsync<JsonElement>($"/staff/pets/{clinic.PetId}/history", Token);

        Assert.Contains(history.GetProperty("data").EnumerateArray(), entry => entry.GetProperty("kind").GetString() == "vaccination");
    }
}
