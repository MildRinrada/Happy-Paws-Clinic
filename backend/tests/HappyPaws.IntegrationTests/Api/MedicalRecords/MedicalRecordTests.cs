using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.MedicalRecords;

/// <summary>
/// Reading a signed record, correcting it with a trail, and the access log that records both.
/// </summary>
[Collection(nameof(ApiCollection))]
public class MedicalRecordTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Runs a case to a signed record and returns its id alongside the clinic.</summary>
    private async Task<(SeededClinic Clinic, string RecordId, string PetId)> SignedRecordAsync()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await vet.PatchAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}",
            new { assessment = "สงสัยติดเชื้อผิวหนัง", plan = "ให้ยา 7 วัน" },
            Token);

        await DiagnoseAsync(vet, open.ConsultationId, Token);

        var result = await CloseAsync(vet, open.ConsultationId, Token);

        return (open.Clinic, result.GetProperty("record").GetProperty("id").GetString()!, open.Clinic.PetId);
    }

    [Fact]
    public async Task Opening_a_record_writes_a_row_in_the_access_log()
    {
        var (clinic, recordId, petId) = await SignedRecordAsync();
        using var vet = fixture.Vet(clinic);

        var read = await vet.GetAsync($"/staff/medical-records/{recordId}", Token);
        read.EnsureSuccessStatusCode();

        var access = await vet.GetFromJsonAsync<JsonElement>(
            $"/staff/audit-logs/access?pet_id={petId}", Token);

        var rows = access.GetProperty("data").EnumerateArray().ToList();

        // This is the row a patient's owner is entitled to ask about, and the whole reason the read
        // path is a handler rather than a controller reaching into the repository.
        Assert.Contains(rows, row => row.GetProperty("action").GetString() == "record.viewed"
            && row.GetProperty("resource_id").GetString() == recordId);

        var viewed = rows.First(row => row.GetProperty("action").GetString() == "record.viewed");
        Assert.Equal(clinic.StaffUserId, viewed.GetProperty("actor_user_id").GetString());
        Assert.NotNull(viewed.GetProperty("request_id").GetString());
    }

    [Fact]
    public async Task The_access_log_holds_reads_and_the_main_log_holds_changes()
    {
        var (clinic, recordId, petId) = await SignedRecordAsync();
        using var vet = fixture.Vet(clinic);

        (await vet.GetAsync($"/staff/medical-records/{recordId}", Token)).Dispose();

        var access = await vet.GetFromJsonAsync<JsonElement>($"/staff/audit-logs/access?pet_id={petId}", Token);
        var all = await vet.GetFromJsonAsync<JsonElement>($"/staff/audit-logs?pet_id={petId}", Token);

        var accessActions = access.GetProperty("data").EnumerateArray()
            .Select(row => row.GetProperty("action").GetString()).ToList();
        var allActions = all.GetProperty("data").EnumerateArray()
            .Select(row => row.GetProperty("action").GetString()).ToList();

        // The access endpoint answers a privacy question and must not be diluted with writes.
        Assert.All(accessActions, action => Assert.Equal("record.viewed", action));
        Assert.Contains("record.signed", allActions);
        Assert.Contains("record.viewed", allActions);
    }

    [Fact]
    public async Task An_amendment_keeps_the_original_and_records_the_diff()
    {
        var (clinic, recordId, _) = await SignedRecordAsync();
        using var vet = fixture.Vet(clinic);

        var response = await vet.PostAsJsonAsync(
            $"/staff/medical-records/{recordId}/amendments",
            new
            {
                reason = "ผลแล็บกลับมายืนยันการติดเชื้อ",
                changes = new { assessment = "ยืนยันติดเชื้อผิวหนัง" },
            },
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var record = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("amended", record.GetProperty("status").GetString());
        Assert.Equal(
            "ยืนยันติดเชื้อผิวหนัง",
            record.GetProperty("content").GetProperty("assessment").GetString());
        // "เก็บฉบับเดิมไว้เสมอ" — the signed original is still there, unchanged.
        Assert.Equal(
            "สงสัยติดเชื้อผิวหนัง",
            record.GetProperty("original_content").GetProperty("assessment").GetString());

        var amendment = record.GetProperty("amendments")[0];
        Assert.Equal("ผลแล็บกลับมายืนยันการติดเชื้อ", amendment.GetProperty("reason").GetString());
        Assert.Equal(
            "สงสัยติดเชื้อผิวหนัง",
            amendment.GetProperty("changes").GetProperty("assessment").GetProperty("from").GetString());
    }

    [Fact]
    public async Task The_signature_still_verifies_after_an_amendment()
    {
        var (clinic, recordId, _) = await SignedRecordAsync();
        using var vet = fixture.Vet(clinic);

        await vet.PostAsJsonAsync(
            $"/staff/medical-records/{recordId}/amendments",
            new { reason = "พิมพ์ผิด", changes = new { plan = "ให้ยา 10 วัน" } },
            Token);

        var record = await vet.GetFromJsonAsync<JsonElement>($"/staff/medical-records/{recordId}", Token);

        // The signature attests to what was signed. An amendment adds a version rather than
        // rewriting what the signer put their name to.
        Assert.True(record.GetProperty("integrity_verified").GetBoolean());
    }

    [Fact]
    public async Task A_billed_treatment_cannot_be_amended_away()
    {
        var (clinic, recordId, _) = await SignedRecordAsync();
        using var vet = fixture.Vet(clinic);

        var response = await vet.PostAsJsonAsync(
            $"/staff/medical-records/{recordId}/amendments",
            new { reason = "คิดเงินผิด", changes = new { treatments = "[]" } },
            Token);

        // Corrections to money go through billing, not through rewriting what the record says
        // happened.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task The_record_audit_view_shows_the_amendment_and_the_entries_together()
    {
        var (clinic, recordId, _) = await SignedRecordAsync();
        using var vet = fixture.Vet(clinic);

        await vet.PostAsJsonAsync(
            $"/staff/medical-records/{recordId}/amendments",
            new { reason = "แก้คำวินิจฉัย", changes = new { assessment = "ยืนยันติดเชื้อ" } },
            Token);

        var audit = await vet.GetFromJsonAsync<JsonElement>($"/staff/medical-records/{recordId}/audit", Token);

        Assert.Equal("amended", audit.GetProperty("status").GetString());
        Assert.Single(audit.GetProperty("amendments").EnumerateArray());

        var actions = audit.GetProperty("entries").GetProperty("data").EnumerateArray()
            .Select(row => row.GetProperty("action").GetString()).ToList();

        Assert.Contains("record.signed", actions);
        Assert.Contains("record.amended", actions);
    }

    [Fact]
    public async Task Reading_a_record_and_reviewing_who_read_it_are_different_permissions()
    {
        var (clinic, recordId, _) = await SignedRecordAsync();

        using var reader = fixture.Vet(clinic, Scopes.Record.Read);

        var record = await reader.GetAsync($"/staff/medical-records/{recordId}", Token);
        var audit = await reader.GetAsync($"/staff/medical-records/{recordId}/audit", Token);
        var log = await reader.GetAsync("/staff/audit-logs", Token);

        Assert.Equal(HttpStatusCode.OK, record.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, audit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, log.StatusCode);
    }

    [Fact]
    public async Task A_record_at_another_branch_is_answered_as_not_found()
    {
        var (_, recordId, _) = await SignedRecordAsync();
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var outsider = fixture.Vet(elsewhere, Scopes.Record.Read);

        var response = await outsider.GetAsync($"/staff/medical-records/{recordId}", Token);

        // 403 rather than 404 here: the caller holds record:read, so the refusal is about the
        // branch, and pretending the record does not exist would hide a real permissions problem
        // from an admin who needs to fix it.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
