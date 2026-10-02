using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>
/// Who may see and change which appointment — the three gates (authenticated, scope, resource)
/// exercised over the wire on the routes the contract shares between both audiences.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AppointmentAccessTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<(SeededClinic Clinic, string AppointmentId)> BookedAsync(int hoursFromNow)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        return (clinic, await BookAsStaffAsync(fixture, clinic, hoursFromNow, Token));
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_read_an_appointment()
    {
        var (_, appointmentId) = await BookedAsync(4);

        using var anonymous = fixture.CreateClient();
        var response = await anonymous.GetAsync($"/appointments/{appointmentId}", Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_reads_their_own_pets_appointment()
    {
        var (clinic, appointmentId) = await BookedAsync(5);

        using var customer = fixture.Authenticated(
            TestTokens.Customer(fixture, clinic.CustomerUserId, clinic.CustomerId));

        var response = await customer.GetAsync($"/appointments/{appointmentId}", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task Another_customers_appointment_is_hidden_behind_a_404_not_a_403()
    {
        // Appointment ids appear in URLs and in LINE messages. A 403 would confirm which ones are
        // real and turn this endpoint into a census of the clinic's bookings.
        var (_, appointmentId) = await BookedAsync(6);
        var (otherCustomerId, _) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using var stranger = fixture.Authenticated(
            TestTokens.Customer(fixture, "usr_stranger", otherCustomerId));

        var response = await stranger.GetAsync($"/appointments/{appointmentId}", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_cannot_cancel_another_customers_appointment()
    {
        var (_, appointmentId) = await BookedAsync(7);
        var (otherCustomerId, _) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using var stranger = fixture.Authenticated(
            TestTokens.Customer(fixture, "usr_stranger", otherCustomerId));

        var response = await stranger.PostAsJsonAsync(
            $"/appointments/{appointmentId}/cancel", new { reason = "ไม่ใช่ของฉัน" }, Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var stored = await ClinicSeed.ReloadAppointmentAsync(fixture, appointmentId, Token);
        Assert.NotNull(stored);
        Assert.True(stored.OccupiesSlot);
    }

    [Fact]
    public async Task Staff_at_another_branch_are_refused()
    {
        var (_, appointmentId) = await BookedAsync(8);

        using var elsewhere = fixture.Authenticated(TestTokens.Staff(
            fixture,
            scopes: [Scopes.Appointment.Read, Scopes.Appointment.Write],
            branches: ["brn_somewhere_else"]));

        var response = await elsewhere.GetAsync($"/appointments/{appointmentId}", Token);

        // Staff already know the clinic's appointments exist, so this is a 403 about the record —
        // not the 404 a customer gets.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_staff_board_needs_appointment_read_and_says_what_is_missing()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var unscoped = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Queue.Read], [clinic.BranchId]));

        var response = await unscoped.GetAsync($"/staff/appointments?branch={clinic.BranchId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

        Assert.Equal("insufficient_scope", error.GetProperty("code").GetString());
        Assert.Equal(
            "appointment:read",
            error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Single().GetString());
    }

    [Fact]
    public async Task The_staff_board_lists_the_day_and_pages_by_cursor()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId,
            [Scopes.Appointment.Read, Scopes.Appointment.Write], [clinic.BranchId]));

        for (var hour = 3; hour <= 5; hour++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/staff/appointments")
            {
                Content = JsonContent.Create(new
                {
                    pet_id = clinic.PetId,
                    service_id = clinic.ServiceId,
                    starts_at = ClinicSeed.SlotAt(hour),
                    branch_id = clinic.BranchId,
                }),
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            (await staff.SendAsync(request, Token)).EnsureSuccessStatusCode();
        }

        var first = await staff.GetAsync($"/staff/appointments?branch={clinic.BranchId}&limit=2", Token);
        var firstPage = await first.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(2, firstPage.GetProperty("data").GetArrayLength());

        var cursor = firstPage.GetProperty("page").GetProperty("next_cursor").GetString();
        Assert.NotNull(cursor);

        var second = await staff.GetAsync(
            $"/staff/appointments?branch={clinic.BranchId}&limit=2&cursor={Uri.EscapeDataString(cursor)}", Token);
        var secondPage = await second.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(1, secondPage.GetProperty("data").GetArrayLength());
        // Last page: no cursor pointing at nothing.
        Assert.False(secondPage.GetProperty("page").TryGetProperty("next_cursor", out _));

        // No row appears on both pages.
        var firstIds = firstPage.GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString());
        var secondIds = secondPage.GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString());
        Assert.Empty(firstIds.Intersect(secondIds));
    }

    [Fact]
    public async Task An_unknown_status_filter_is_refused_rather_than_ignored()
    {
        // Ignoring it would answer with the whole board and look like a successful query.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Appointment.Read], [clinic.BranchId]));

        var response = await staff.GetAsync(
            $"/staff/appointments?branch={clinic.BranchId}&status=definitely_not_a_status", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("status", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Patching_without_if_match_is_refused()
    {
        var (clinic, appointmentId) = await BookedAsync(9);

        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Appointment.Write], [clinic.BranchId]));

        var response = await staff.PatchAsJsonAsync(
            $"/staff/appointments/{appointmentId}", new { reason = "อาการใหม่" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("validation_failed", error.GetProperty("code").GetString());
        Assert.Equal("If-Match", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_stale_if_match_answers_409_and_leaves_the_record_alone()
    {
        var (clinic, appointmentId) = await BookedAsync(10);

        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId,
            [Scopes.Appointment.Read, Scopes.Appointment.Write], [clinic.BranchId]));

        var read = await staff.GetAsync($"/appointments/{appointmentId}", Token);
        var version = (await read.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("version").GetUInt32();

        using var patch = new HttpRequestMessage(HttpMethod.Patch, $"/staff/appointments/{appointmentId}")
        {
            Content = JsonContent.Create(new { reason = "แก้ไขจากสำเนาที่เก่าแล้ว" }),
        };
        patch.Headers.TryAddWithoutValidation("If-Match", $"\"{version + 1}\"");

        var response = await staff.SendAsync(patch, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("version_conflict", error.GetProperty("code").GetString());
        Assert.Equal(version, error.GetProperty("details").GetProperty("current_version").GetUInt32());

        var stored = await ClinicSeed.ReloadAppointmentAsync(fixture, appointmentId, Token);
        Assert.Null(stored!.Reason);
    }

    [Fact]
    public async Task A_current_if_match_applies_the_patch_and_returns_the_new_version()
    {
        var (clinic, appointmentId) = await BookedAsync(11);

        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId,
            [Scopes.Appointment.Read, Scopes.Appointment.Write], [clinic.BranchId]));

        var read = await staff.GetAsync($"/appointments/{appointmentId}", Token);
        var version = (await read.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("version").GetUInt32();

        using var patch = new HttpRequestMessage(HttpMethod.Patch, $"/staff/appointments/{appointmentId}")
        {
            Content = JsonContent.Create(new { reason = "เพิ่มอาการ: อาเจียนเมื่อคืน" }),
        };
        patch.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");

        var response = await staff.SendAsync(patch, Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("เพิ่มอาการ: อาเจียนเมื่อคืน", updated.GetProperty("reason").GetString());
        Assert.NotEqual(version, updated.GetProperty("version").GetUInt32());
    }

    [Fact]
    public async Task The_portal_list_only_ever_returns_the_callers_own_appointments()
    {
        var (clinic, appointmentId) = await BookedAsync(12);
        var (otherCustomerId, _) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using var owner = fixture.Authenticated(
            TestTokens.Customer(fixture, clinic.CustomerUserId, clinic.CustomerId));

        var mine = await owner.GetAsync("/portal/appointments?upcoming=true", Token);
        var mineIds = (await mine.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString());

        Assert.Contains(appointmentId, mineIds);

        using var stranger = fixture.Authenticated(
            TestTokens.Customer(fixture, "usr_stranger", otherCustomerId));

        var theirs = await stranger.GetAsync("/portal/appointments?upcoming=true", Token);
        var theirIds = (await theirs.Content.ReadFromJsonAsync<JsonElement>(Token))
            .GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString());

        Assert.DoesNotContain(appointmentId, theirIds);
    }
}
