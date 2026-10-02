using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Consultations;

/// <summary>
/// "ตรวจแพ้ยา" — what the prescription endpoint does when the patient is allergic to what is
/// being written, and what it deliberately does not do.
/// </summary>
[Collection(nameof(ApiCollection))]
public class PrescriptionAllergyTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>The drug the seeded patient is allergic to, optionally with an acknowledgement.</summary>
    private static object Amoxicillin(string? acknowledge = null) => new
    {
        drug_name = "Amoxicillin 250mg",
        dose = "1 เม็ด",
        frequency = "วันละ 2 ครั้ง หลังอาหาร",
        duration_days = 7,
        quantity = 14,
        acknowledge_allergy = acknowledge,
    };

    private async Task<OpenCase> CaseWithAllergyAsync(string severity, string substance = "Amoxicillin")
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        var response = await vet.PostAsJsonAsync(
            $"/staff/pets/{open.Clinic.PetId}/allergies",
            new { substance, severity, reaction = "ผื่นแดง หายใจลำบาก" },
            Token);

        response.EnsureSuccessStatusCode();

        return open;
    }

    private Task<HttpResponseMessage> PrescribeAsync(OpenCase open, HttpClient vet, object body)
    {
        var request = BookingScenario.Idempotent(
            $"/staff/consultations/{open.ConsultationId}/prescriptions", body);

        return vet.SendAsync(request, Token);
    }

    [Fact]
    public async Task A_drug_the_patient_is_allergic_to_is_refused_and_the_allergy_is_named()
    {
        var open = await CaseWithAllergyAsync("moderate");
        using var vet = fixture.Vet(open.Clinic);

        var response = await PrescribeAsync(open, vet, Amoxicillin());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

        Assert.Equal("allergy_conflict", error.GetProperty("code").GetString());
        Assert.Equal("drug_name", error.GetProperty("field").GetString());

        var details = error.GetProperty("details");
        Assert.True(details.GetProperty("overridable").GetBoolean());
        Assert.Equal("Amoxicillin", details.GetProperty("allergies")[0].GetString());
    }

    [Fact]
    public async Task An_acknowledged_allergy_lets_the_vet_prescribe_and_records_the_decision()
    {
        var open = await CaseWithAllergyAsync("moderate");
        using var vet = fixture.Vet(open.Clinic);

        var response = await PrescribeAsync(
            open, vet, Amoxicillin("ผื่นเล็กน้อยเมื่อ 3 ปีก่อน · จำเป็นต้องใช้ · เฝ้าระวังอาการ"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var consultation = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var prescription = consultation.GetProperty("prescriptions")[0];

        // The decision stays with the vet; the record shows it was made knowingly.
        Assert.StartsWith("ผื่นเล็กน้อย", prescription.GetProperty("allergy_acknowledgement").GetString());

        var log = await vet.GetFromJsonAsync<JsonElement>(
            $"/staff/audit-logs?pet_id={open.Clinic.PetId}&action=consultation.allergy_overridden", Token);

        var entry = log.GetProperty("data")[0];
        Assert.Equal("Amoxicillin 250mg", entry.GetProperty("details").GetProperty("drug_name").GetString());
    }

    [Fact]
    public async Task A_severe_allergy_cannot_be_overridden_through_this_endpoint()
    {
        var open = await CaseWithAllergyAsync("severe");
        using var vet = fixture.Vet(open.Clinic);

        var response = await PrescribeAsync(open, vet, Amoxicillin("จำเป็น"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.False(error.GetProperty("details").GetProperty("overridable").GetBoolean());
    }

    [Fact]
    public async Task A_drug_the_patient_is_not_allergic_to_goes_straight_through()
    {
        var open = await CaseWithAllergyAsync("severe", substance: "Penicillin");
        using var vet = fixture.Vet(open.Clinic);

        var response = await PrescribeAsync(open, vet, new
        {
            drug_name = "Meloxicam",
            dose = "0.5 ml",
            frequency = "วันละครั้ง",
            duration_days = 3,
            quantity = 3,
        });

        // The check matches on name and knows nothing about drug classes — an honest limit, and
        // the reason this test names a drug that is unrelated by any measure rather than one that
        // merely happens to be in a different family.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_withdrawn_allergy_no_longer_blocks_prescribing()
    {
        var open = await CaseWithAllergyAsync("moderate");
        using var vet = fixture.Vet(open.Clinic);

        Assert.Equal(HttpStatusCode.Conflict, (await PrescribeAsync(open, vet, Amoxicillin())).StatusCode);

        await PatientSeed.WithdrawAllergiesAsync(fixture, open.Clinic.PetId, Token);

        // Withdrawn, not deleted: "this patient was believed allergic for two years" stays in the
        // table, it simply stops gating new prescriptions.
        Assert.Equal(HttpStatusCode.OK, (await PrescribeAsync(open, vet, Amoxicillin())).StatusCode);
    }
}
