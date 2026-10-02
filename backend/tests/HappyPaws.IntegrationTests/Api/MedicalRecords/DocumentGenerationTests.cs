using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.MedicalRecords;

/// <summary>"ใบรับรองสุขภาพ ใบส่งตัว สมุดวัคซีน เป็น PDF".</summary>
[Collection(nameof(ApiCollection))]
public class DocumentGenerationTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] Readers = [Scopes.Record.Read, Scopes.Audit.Read, .. ClinicalScopes];

    [Fact]
    public async Task A_referral_letter_from_a_signed_visit_is_a_pdf_and_is_on_the_access_log()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic, Readers);
        await DiagnoseAsync(vet, open.ConsultationId, Token);
        await CloseAsync(vet, open.ConsultationId, Token);

        var response = await vet.PostAsJsonAsync(
            "/staff/documents/generate",
            new { type = "referral_letter", pet_id = open.Clinic.PetId, refer_to = "โรงพยาบาลสัตว์ทดสอบ", reason = "ส่งตรวจ CT" },
            Token);

        response.EnsureSuccessStatusCode();
        var pdf = await response.Content.ReadAsByteArrayAsync(Token);

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));

        var log = await vet.GetFromJsonAsync<JsonElement>(
            $"/staff/audit-logs/access?pet_id={open.Clinic.PetId}", Token);

        Assert.Contains(log.GetProperty("data").EnumerateArray(), entry => entry.GetProperty("action").GetString() == "pet.document_generated");
    }

    [Fact]
    public async Task A_vaccination_book_prints_for_a_pet_with_no_visits()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic, Readers);

        var response = await vet.PostAsJsonAsync("/staff/documents/generate", new { type = "vaccination_book", pet_id = clinic.PetId }, Token);

        response.EnsureSuccessStatusCode();
        Assert.Equal("%PDF", Encoding.ASCII.GetString(await response.Content.ReadAsByteArrayAsync(Token), 0, 4));
    }

    [Fact]
    public async Task A_referral_needs_a_signed_visit_to_refer()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic, Readers);

        var response = await vet.PostAsJsonAsync(
            "/staff/documents/generate",
            new { type = "referral_letter", pet_id = clinic.PetId, refer_to = "ที่ไหนสักแห่ง", reason = "ทดสอบ" },
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_certificate_needs_a_signer_with_a_licence()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        // A token for a staff id with no staff record behind it, so no licence number either.
        using var unlicensed = fixture.Authenticated(TestTokens.Staff(fixture, scopes: Readers, branches: [clinic.BranchId]));

        var response = await unlicensed.PostAsJsonAsync(
            "/staff/documents/generate", new { type = "health_certificate", pet_id = clinic.PetId }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
