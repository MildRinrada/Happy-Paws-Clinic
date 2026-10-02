using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Customers;

/// <summary>
/// Data-subject requests: received, timed, and answered.
/// <para>
/// Nothing here deletes anything. An erasure request against a signed medical record collides with
/// a retention duty, and reconciling the two is a judgement a person makes and is accountable for.
/// What the API guarantees is the part that is currently impossible — that the request exists, with
/// a date on it, and that the answer is recorded. See <c>docs/contract-extensions.md § 4</c>.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class DataRequestTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Officer(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Customer.Read, Scopes.Customer.Write, Scopes.Audit.Read] : scopes,
            [clinic.BranchId]));

    private async Task<(SeededClinic Clinic, string RequestId)> RaisedAsync(string type = "erasure")
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        var response = await owner.PostAsJsonAsync(
            "/portal/data-requests",
            new { type, detail = "ขอให้ลบข้อมูลส่วนบุคคลทั้งหมด" },
            Token);

        response.EnsureSuccessStatusCode();

        var raised = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        return (clinic, raised.GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task A_customer_can_raise_a_request_and_it_starts_open_with_a_clock_on_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        var response = await owner.PostAsJsonAsync(
            "/portal/data-requests", new { type = "access", detail = "ขอดูข้อมูลทั้งหมดที่เก็บไว้" }, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var raised = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.StartsWith("dsr_", raised.GetProperty("id").GetString(), StringComparison.Ordinal);
        Assert.Equal("access", raised.GetProperty("type").GetString());
        Assert.Equal("open", raised.GetProperty("status").GetString());
        Assert.Equal(clinic.CustomerId, raised.GetProperty("customer_id").GetString());
        // The clock is the substance: compliance is measured in days from receipt.
        Assert.Equal(0, raised.GetProperty("open_days").GetInt32());
    }

    [Fact]
    public async Task The_clinic_sees_outstanding_requests_oldest_first()
    {
        var (clinic, first) = await RaisedAsync();

        using var owner = fixture.CustomerOf(clinic);
        var secondResponse = await owner.PostAsJsonAsync(
            "/portal/data-requests", new { type = "correction" }, Token);
        var second = (await secondResponse.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("id").GetString();

        using var officer = Officer(clinic);
        var list = await officer.GetFromJsonAsync<JsonElement>(
            $"/staff/data-requests?status=open&customer_id={clinic.CustomerId}", Token);

        var ids = list.EnumerateArray().Select(row => row.GetProperty("id").GetString()).ToList();

        // Oldest first, unlike every other list here — the top of this one is the request closest
        // to a statutory deadline.
        Assert.Equal([first, second], ids);
    }

    [Fact]
    public async Task Closing_a_request_records_what_was_done_and_who_did_it()
    {
        var (clinic, requestId) = await RaisedAsync();
        using var officer = Officer(clinic);

        var response = await officer.PostAsJsonAsync(
            $"/staff/data-requests/{requestId}/close",
            new { outcome = "completed", resolution = "ลบข้อมูลติดต่อและปิดบัญชี portal เมื่อ 27 ส.ค." },
            Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var closed = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("completed", closed.GetProperty("status").GetString());
        Assert.Equal(clinic.StaffUserId, closed.GetProperty("closed_by_user_id").GetString());
        Assert.StartsWith("ลบข้อมูลติดต่อ", closed.GetProperty("resolution").GetString());
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task Refusing_is_a_recorded_outcome_and_not_an_error()
    {
        var (clinic, requestId) = await RaisedAsync();
        using var officer = Officer(clinic);

        var response = await officer.PostAsJsonAsync(
            $"/staff/data-requests/{requestId}/close",
            new
            {
                outcome = "refused",
                resolution = "เวชระเบียนที่ลงนามแล้วเป็นหลักฐานการรักษา เก็บตามระยะเวลาที่กฎหมายกำหนด",
            },
            Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var closed = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("refused", closed.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Closing_without_saying_what_was_done_is_refused()
    {
        var (clinic, requestId) = await RaisedAsync();
        using var officer = Officer(clinic);

        var response = await officer.PostAsJsonAsync(
            $"/staff/data-requests/{requestId}/close", new { outcome = "completed", resolution = "  " }, Token);

        // A closed request with no stated outcome is indistinguishable from an ignored one.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_request_cannot_be_closed_twice()
    {
        var (clinic, requestId) = await RaisedAsync();
        using var officer = Officer(clinic);

        await officer.PostAsJsonAsync(
            $"/staff/data-requests/{requestId}/close",
            new { outcome = "completed", resolution = "ดำเนินการแล้ว" },
            Token);

        var again = await officer.PostAsJsonAsync(
            $"/staff/data-requests/{requestId}/close",
            new { outcome = "refused", resolution = "เปลี่ยนใจ" },
            Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Raising_and_closing_are_both_audited()
    {
        var (clinic, requestId) = await RaisedAsync();
        using var officer = Officer(clinic);

        await officer.PostAsJsonAsync(
            $"/staff/data-requests/{requestId}/close",
            new { outcome = "refused", resolution = "เก็บตามกฎหมาย" },
            Token);

        var log = await officer.GetFromJsonAsync<JsonElement>(
            $"/staff/audit-logs?resource_id={requestId}", Token);

        var actions = log.GetProperty("data").EnumerateArray()
            .Select(row => row.GetProperty("action").GetString()).ToList();

        Assert.Contains("data_request.raised", actions);
        Assert.Contains("data_request.closed", actions);
    }

    [Fact]
    public async Task An_unknown_request_type_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        var response = await owner.PostAsJsonAsync("/portal/data-requests", new { type = "delete_everything" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_cannot_close_their_own_request()
    {
        var (clinic, requestId) = await RaisedAsync();
        using var owner = fixture.CustomerOf(clinic);

        var response = await owner.PostAsJsonAsync(
            $"/staff/data-requests/{requestId}/close",
            new { outcome = "completed", resolution = "ทำเองแล้ว" },
            Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
