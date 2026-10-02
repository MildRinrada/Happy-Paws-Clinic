using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Consultations.SeedTreatmentTemplates;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Prescriptions;

/// <summary>"ยาที่เคยจ่ายทั้งหมด · ตรวจซ้ำซ้อน", "จ่ายยาจริง · ตัดสต๊อกและพิมพ์ฉลาก", and the treatment templates.</summary>
[Collection(nameof(ApiCollection))]
public class PharmacyTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] PharmacyScopes =
        [Scopes.Prescription.Read, Scopes.Prescription.Dispense, Scopes.Consult.Read, .. ClinicalScopes];

    private static async Task<string> PrescribeAsync(HttpClient vet, string consultationId, string drug, int days = 7)
    {
        using var request = BookingScenario.Idempotent(
            $"/staff/consultations/{consultationId}/prescriptions",
            new { drug_name = drug, dose = "1 เม็ด", frequency = "วันละ 2 ครั้ง", duration_days = days, quantity = days * 2 });

        var response = await vet.SendAsync(request, Token);
        response.EnsureSuccessStatusCode();

        var consultation = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        return consultation.GetProperty("prescriptions").EnumerateArray().Last().GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task The_same_drug_on_overlapping_courses_is_flagged_as_a_duplicate()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic, PharmacyScopes);

        var first = await PrescribeAsync(vet, open.ConsultationId, "Amoxicillin 250mg");
        var second = await PrescribeAsync(vet, open.ConsultationId, "amoxicillin 500 mg");
        var other = await PrescribeAsync(vet, open.ConsultationId, "Meloxicam");

        var list = await vet.GetFromJsonAsync<JsonElement>($"/staff/prescriptions?pet_id={open.Clinic.PetId}", Token);
        var byId = list.GetProperty("data").EnumerateArray().ToDictionary(item => item.GetProperty("id").GetString()!);

        Assert.Equal(second, byId[first].GetProperty("duplicate_of")[0].GetString());
        Assert.Equal(first, byId[second].GetProperty("duplicate_of")[0].GetString());
        Assert.Equal(0, byId[other].GetProperty("duplicate_of").GetArrayLength());
        Assert.True(byId[first].GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Dispensing_answers_with_the_label_and_says_stock_was_not_touched()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic, PharmacyScopes);
        var rx = await PrescribeAsync(vet, open.ConsultationId, "Meloxicam");

        var response = await vet.PostAsync($"/staff/prescriptions/{rx}/dispense", null, Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var label = result.GetProperty("label");

        Assert.False(result.GetProperty("stock_deducted").GetBoolean());
        Assert.NotNull(result.GetProperty("prescription").GetProperty("dispensed_at").GetString());
        Assert.Equal("Meloxicam", label.GetProperty("drug_name").GetString());
        Assert.Equal("ลูน่า", label.GetProperty("pet_name").GetString());
        Assert.Equal("ส.พ. 06120", label.GetProperty("prescriber_license_no").GetString());
    }

    [Fact]
    public async Task A_prescription_is_dispensed_once()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic, PharmacyScopes);
        var rx = await PrescribeAsync(vet, open.ConsultationId, "Meloxicam");

        (await vet.PostAsync($"/staff/prescriptions/{rx}/dispense", null, Token)).EnsureSuccessStatusCode();
        var again = await vet.PostAsync($"/staff/prescriptions/{rx}/dispense", null, Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Writing_a_prescription_does_not_let_you_dispense_it()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);
        var rx = await PrescribeAsync(vet, open.ConsultationId, "Meloxicam");

        var response = await vet.PostAsync($"/staff/prescriptions/{rx}/dispense", null, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_templates_are_listed_and_narrowed_by_species()
    {
        using (var scope = fixture.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<SeedTreatmentTemplatesHandler>().HandleAsync(Token);
        }

        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic, Scopes.Consult.Read);

        var cats = await vet.GetFromJsonAsync<JsonElement>("/staff/templates/treatments?species=แมว", Token);
        var names = cats.GetProperty("data").EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToList();

        Assert.Contains("ทำหมันแมวเพศเมีย", names);
        Assert.Contains("ท้องเสียเฉียบพลัน", names);
        Assert.DoesNotContain("วัคซีนลูกสุนัข 3 เข็ม", names);
    }
}
