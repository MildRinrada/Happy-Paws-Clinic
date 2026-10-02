using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Clinics;

/// <summary>
/// The clinic's operating rules, and proof that each one does something.
/// <para>
/// A setting that is stored and returned but changes nothing is worse than no setting: the screen
/// says the clinic has a rule and the system does not have it. So every test here changes a
/// setting and then watches a different endpoint behave differently.
/// </para>
/// <para>
/// On the separate instance, and each test puts back what it changed: these rows are the whole
/// clinic's, and the tests in a collection run in no promised order.
/// </para>
/// </summary>
[Collection(nameof(FreshInstanceCollection))]
public class ClinicSettingsTests(ApiFixture fixture) : FreshInstanceTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    protected override ApiFixture Instance => fixture;

    private HttpClient Admin(params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: scopes.Length == 0 ? [Scopes.Settings.Read, Scopes.Settings.Write] : scopes));

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private static async Task<JsonElement> PatchAsync(HttpClient client, string path, object body)
    {
        var response = await client.PatchAsJsonAsync(path, body, Token);
        response.EnsureSuccessStatusCode();

        return await BodyAsync(response);
    }

    [Fact]
    public async Task Booking_settings_are_read_with_settings_read_and_changed_with_settings_write()
    {
        using var admin = Admin();
        using var reader = Admin(Scopes.Settings.Read);

        var before = await reader.GetFromJsonAsync<JsonElement>("/clinic/settings/booking", Token);

        // The contract's own examples: "slot 15/20/30 นาที".
        Assert.Contains(before.GetProperty("slot_minutes").GetInt32(), new[] { 15, 20, 30 });

        try
        {
            var changed = await PatchAsync(admin, "/clinic/settings/booking", new { cancellation_cutoff_hours = 6 });

            Assert.Equal(6, changed.GetProperty("cancellation_cutoff_hours").GetInt32());

            // PATCH: the fields that were not sent are what they were.
            Assert.Equal(before.GetProperty("slot_minutes").GetInt32(), changed.GetProperty("slot_minutes").GetInt32());
            Assert.Equal(before.GetProperty("lead_time_minutes").GetInt32(), changed.GetProperty("lead_time_minutes").GetInt32());

            Assert.Equal(
                HttpStatusCode.Forbidden,
                (await reader.PatchAsJsonAsync("/clinic/settings/booking", new { slot_minutes = 15 }, Token)).StatusCode);
        }
        finally
        {
            await PatchAsync(admin, "/clinic/settings/booking", new
            {
                cancellation_cutoff_hours = before.GetProperty("cancellation_cutoff_hours").GetInt32(),
            });
        }
    }

    [Theory]
    [InlineData("slot_minutes", 7)]
    [InlineData("slot_minutes", 0)]
    [InlineData("max_advance_days", 0)]
    [InlineData("lead_time_minutes", -5)]
    public async Task A_booking_rule_outside_its_bounds_is_a_validation_error(string field, int value)
    {
        using var admin = Admin();

        var response = await admin.PatchAsJsonAsync(
            "/clinic/settings/booking", new Dictionary<string, int> { [field] = value }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(field, (await BodyAsync(response)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_stale_version_does_not_overwrite_someone_elses_change()
    {
        using var admin = Admin();

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/clinic/settings/booking")
        {
            Content = JsonContent.Create(new { max_advance_days = 90 }),
        };

        request.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        var response = await admin.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("version_conflict", (await BodyAsync(response)).GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task An_empty_patch_reads_the_queue_settings_without_writing()
    {
        using var admin = Admin();

        // The contract gives this setting a PATCH and no GET.
        var settings = await PatchAsync(admin, "/clinic/settings/queue", new { });

        Assert.False(string.IsNullOrEmpty(settings.GetProperty("example").GetString()));
        Assert.InRange(settings.GetProperty("number_digits").GetInt32(), 2, 5);
    }

    [Fact]
    public async Task The_queue_number_format_is_what_the_next_ticket_is_printed_with()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var admin = Admin();
        using var counter = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Queue.Read, Scopes.Queue.Write], [clinic.BranchId]));

        var before = await PatchAsync(admin, "/clinic/settings/queue", new { });

        try
        {
            var changed = await PatchAsync(admin, "/clinic/settings/queue", new { number_prefix = "q", number_digits = 2 });

            // "รูปแบบเลขคิว".
            Assert.Equal("Q", changed.GetProperty("number_prefix").GetString());
            Assert.Equal("Q14", changed.GetProperty("example").GetString());

            using var request = Idempotent("/staff/queue/tickets", new
            {
                pet_id = clinic.PetId,
                service_id = clinic.ServiceId,
                branch_id = clinic.BranchId,
            });

            var issued = await counter.SendAsync(request, Token);
            issued.EnsureSuccessStatusCode();

            Assert.Equal("Q01", (await BodyAsync(issued)).GetProperty("number").GetString());
        }
        finally
        {
            await PatchAsync(admin, "/clinic/settings/queue", new
            {
                number_prefix = before.GetProperty("number_prefix").GetString(),
                number_digits = before.GetProperty("number_digits").GetInt32(),
            });
        }
    }

    [Theory]
    [InlineData("ABCD", 3)]
    [InlineData("A1", 3)]
    [InlineData("A", 9)]
    public async Task A_queue_format_that_cannot_be_printed_or_called_out_is_refused(string prefix, int digits)
    {
        using var admin = Admin();

        var response = await admin.PatchAsJsonAsync(
            "/clinic/settings/queue", new { number_prefix = prefix, number_digits = digits }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_field_the_clinic_requires_stops_a_case_being_signed_without_it()
    {
        using var admin = Admin();

        try
        {
            await PatchAsync(admin, "/clinic/settings/medical-record", new { required_fields = new[] { "vitals", "plan" } });

            var opened = await ConsultationScenario.OpenCaseAsync(fixture, Token);
            using var vet = fixture.Vet(opened.Clinic);

            await ConsultationScenario.DiagnoseAsync(vet, opened.ConsultationId, Token);

            using var close = Idempotent($"/staff/consultations/{opened.ConsultationId}/close", new { });
            var refused = await vet.SendAsync(close, Token);

            // "ฟิลด์บังคับ". A 409: the request is fine, the case is not ready.
            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

            var error = (await BodyAsync(refused)).GetProperty("error");

            Assert.Equal("incomplete_consultation", error.GetProperty("code").GetString());
            Assert.Equal(
                ["vitals", "plan"],
                error.GetProperty("details").GetProperty("missing_fields").EnumerateArray().Select(f => f.GetString()));

            (await vet.PostAsJsonAsync(
                $"/staff/consultations/{opened.ConsultationId}/vitals", new { weight_kg = 4.2 }, Token)).EnsureSuccessStatusCode();

            (await vet.PatchAsJsonAsync(
                $"/staff/consultations/{opened.ConsultationId}", new { plan = "นัดติดตามผล 7 วัน" }, Token)).EnsureSuccessStatusCode();

            var closed = await ConsultationScenario.CloseAsync(vet, opened.ConsultationId, Token);

            Assert.Equal("signed", closed.GetProperty("record").GetProperty("status").GetString());
        }
        finally
        {
            await PatchAsync(admin, "/clinic/settings/medical-record", new { required_fields = Array.Empty<string>() });
        }
    }

    [Fact]
    public async Task A_configured_examination_form_refuses_a_system_that_is_not_on_it()
    {
        using var admin = Admin();

        try
        {
            await PatchAsync(admin, "/clinic/settings/medical-record", new { examination_systems = new[] { "skin", "eyes" } });

            var opened = await ConsultationScenario.OpenCaseAsync(fixture, Token);
            using var vet = fixture.Vet(opened.Clinic);

            var path = $"/staff/consultations/{opened.ConsultationId}/examination";

            var offForm = await vet.PostAsJsonAsync(
                path, new { findings = new[] { new { body_system = "cardiovascular", is_normal = true } } }, Token);

            // "แบบฟอร์มตรวจ".
            Assert.Equal(HttpStatusCode.UnprocessableEntity, offForm.StatusCode);

            var onForm = await vet.PostAsJsonAsync(
                path,
                new { findings = new[] { new { body_system = "skin", is_normal = false, note = "ผื่นแดงบริเวณท้อง" } } },
                Token);

            Assert.Equal(HttpStatusCode.OK, onForm.StatusCode);
        }
        finally
        {
            await PatchAsync(admin, "/clinic/settings/medical-record", new { examination_systems = Array.Empty<string>() });
        }
    }

    [Fact]
    public async Task When_the_clinic_says_so_only_the_case_vet_may_sign()
    {
        using var admin = Admin();

        try
        {
            await PatchAsync(admin, "/clinic/settings/medical-record", new { signer_must_be_case_vet = true });

            var opened = await ConsultationScenario.OpenCaseAsync(fixture, Token);
            using var caseVet = fixture.Vet(opened.Clinic);

            await ConsultationScenario.DiagnoseAsync(caseVet, opened.ConsultationId, Token);

            // A colleague at the same branch, holding record:sign, who did not open the case.
            using var colleague = fixture.Authenticated(TestTokens.Staff(
                fixture, "usr_colleague", "stf_colleague", ConsultationScenario.ClinicalScopes, [opened.Clinic.BranchId]));

            using var byColleague = Idempotent($"/staff/consultations/{opened.ConsultationId}/close", new { });

            // "การ sign".
            Assert.Equal(HttpStatusCode.Forbidden, (await colleague.SendAsync(byColleague, Token)).StatusCode);

            var closed = await ConsultationScenario.CloseAsync(caseVet, opened.ConsultationId, Token);

            Assert.Equal("signed", closed.GetProperty("record").GetProperty("status").GetString());
        }
        finally
        {
            await PatchAsync(admin, "/clinic/settings/medical-record", new { signer_must_be_case_vet = false });
        }
    }

    [Fact]
    public async Task A_change_to_the_record_rules_is_audited_with_its_before_and_after()
    {
        using var admin = Admin(Scopes.Settings.Write, Scopes.Audit.Read);

        try
        {
            await PatchAsync(admin, "/clinic/settings/medical-record", new { owner_visible_sections = new[] { "diagnosis" } });

            var log = await admin.GetFromJsonAsync<JsonElement>(
                "/staff/audit-logs?action=settings.medical_record_changed&limit=1", Token);

            var entry = log.GetProperty("data").EnumerateArray().First();

            // "When did we start showing owners the diagnosis" is a question a review will ask.
            Assert.Equal("settings.medical_record_changed", entry.GetProperty("action").GetString());
            Assert.Contains("diagnosis", entry.GetRawText());
        }
        finally
        {
            await PatchAsync(admin, "/clinic/settings/medical-record", new { owner_visible_sections = Array.Empty<string>() });
        }
    }

    [Theory]
    [InlineData("required_fields", "mood")]
    [InlineData("owner_visible_sections", "everything")]
    public async Task A_record_rule_the_system_could_not_enforce_is_refused(string field, string value)
    {
        using var admin = Admin();

        var response = await admin.PatchAsJsonAsync(
            "/clinic/settings/medical-record", new Dictionary<string, string[]> { [field] = [value] }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("/clinic/settings/booking")]
    [InlineData("/clinic/settings/queue")]
    [InlineData("/clinic/settings/medical-record")]
    public async Task Every_settings_write_requires_settings_write(string path)
    {
        using var reader = Admin(Scopes.Settings.Read);

        var response = await reader.PatchAsJsonAsync(path, new { }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
