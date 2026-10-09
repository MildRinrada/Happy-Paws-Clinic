using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>
/// Reading the cancellation policy and the appointment types back, staff and portal side
/// (<c>docs/contract-extensions.md § 23</c>). Tests that change a rule set put it back.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AppointmentRulesReadTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly object DefaultTypes = new
    {
        types = new object[]
        {
            new { code = "regular", name = "นัดปกติ" },
            new { code = "emergency", name = "ฉุกเฉิน", online_booking = false, queue_priority = "emergency" },
            new { code = "follow_up", name = "ติดตามผล" },
            new { code = "surgery", name = "ผ่าตัด", online_booking = false },
        },
    };

    private static readonly object DefaultPolicy = new { cutoff_hours = 24, fee_satang = 0, free_allowance = 1, window_days = 365 };

    private HttpClient Staff(params string[] scopes) => fixture.Authenticated(TestTokens.Staff(fixture, scopes: scopes));

    private static HttpRequestMessage Put(string path, object body, uint version)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return request;
    }

    private static async Task<JsonElement> ErrorOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    [Fact]
    public async Task Reading_the_policy_needs_settings_read()
    {
        using var booker = Staff(Scopes.Appointment.Read, Scopes.Appointment.Write, Scopes.Settings.Write);

        var response = await booker.GetAsync("/staff/policies/cancellation", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await ErrorOf(response);
        Assert.Equal("insufficient_scope", error.GetProperty("code").GetString());
        Assert.Contains(
            error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray(),
            scope => scope.GetString() == Scopes.Settings.Read);
    }

    [Fact]
    public async Task The_policy_reads_back_what_was_saved_and_its_version_guards_the_next_save()
    {
        using var admin = Staff(Scopes.Settings.Read, Scopes.Settings.Write);

        try
        {
            var before = await admin.GetAsync("/staff/policies/cancellation", Token);
            before.EnsureSuccessStatusCode();
            var read = await before.Content.ReadFromJsonAsync<JsonElement>(Token);
            var version = read.GetProperty("version").GetUInt32();

            Assert.Equal($"\"{version}\"", before.Headers.ETag?.Tag);

            var saved = await admin.SendAsync(
                Put("/staff/policies/cancellation", new { cutoff_hours = 6, fee_satang = 15_000, free_allowance = 2, window_days = 180 }, version),
                Token);
            saved.EnsureSuccessStatusCode();

            var after = await admin.GetFromJsonAsync<JsonElement>("/staff/policies/cancellation", Token);
            Assert.Equal(6, after.GetProperty("cutoff_hours").GetInt32());
            Assert.Equal(15_000, after.GetProperty("fee_satang").GetInt64());
            Assert.Equal(2, after.GetProperty("free_allowance").GetInt32());
            Assert.Equal(180, after.GetProperty("window_days").GetInt32());
            Assert.NotEqual(version, after.GetProperty("version").GetUInt32());

            // The version read before the save is now stale.
            var stale = await admin.SendAsync(Put("/staff/policies/cancellation", DefaultPolicy, version), Token);
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        }
        finally
        {
            (await admin.PutAsJsonAsync("/staff/policies/cancellation", DefaultPolicy, Token)).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Reading_the_types_needs_appointment_read()
    {
        using var settingsOnly = Staff(Scopes.Settings.Read, Scopes.Settings.Write);

        var response = await settingsOnly.GetAsync("/staff/appointment-types", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            (await ErrorOf(response)).GetProperty("details").GetProperty("missing_scopes").EnumerateArray(),
            scope => scope.GetString() == Scopes.Appointment.Read);
    }

    [Fact]
    public async Task The_types_read_back_whole_with_a_version_that_guards_the_next_save()
    {
        using var admin = Staff(Scopes.Settings.Write, Scopes.Appointment.Read);

        try
        {
            var before = await admin.GetAsync("/staff/appointment-types", Token);
            before.EnsureSuccessStatusCode();
            var read = await before.Content.ReadFromJsonAsync<JsonElement>(Token);
            var version = read.GetProperty("version").GetUInt32();

            Assert.Equal($"\"{version}\"", before.Headers.ETag?.Tag);
            Assert.Contains(read.GetProperty("types").EnumerateArray(), type => type.GetProperty("code").GetString() == "regular");

            var saved = await admin.SendAsync(
                Put(
                    "/staff/appointment-types",
                    new { types = new object[] { new { code = "regular", name = "นัดปกติ" }, new { code = "dental", name = "ทันตกรรม", duration_minutes = 45, online_booking = false, queue_priority = "urgent" } } },
                    version),
                Token);
            saved.EnsureSuccessStatusCode();

            var after = await admin.GetFromJsonAsync<JsonElement>("/staff/appointment-types", Token);
            var dental = after.GetProperty("types")[1];

            Assert.Equal(2, after.GetProperty("types").GetArrayLength());
            Assert.Equal("dental", dental.GetProperty("code").GetString());
            Assert.Equal(45, dental.GetProperty("duration_minutes").GetInt32());
            Assert.False(dental.GetProperty("online_booking").GetBoolean());
            Assert.Equal("urgent", dental.GetProperty("queue_priority").GetString());

            var stale = await admin.SendAsync(Put("/staff/appointment-types", DefaultTypes, version), Token);
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        }
        finally
        {
            (await admin.PutAsJsonAsync("/staff/appointment-types", DefaultTypes, Token)).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task A_customer_sees_only_the_types_they_may_book_online()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var customer = fixture.CustomerOf(clinic);
        using var admin = Staff(Scopes.Settings.Write);

        try
        {
            var defaults = await customer.GetFromJsonAsync<JsonElement>("/portal/appointment-types", Token);
            var codes = defaults.GetProperty("data").EnumerateArray().Select(type => type.GetProperty("code").GetString()).ToList();

            Assert.Equal(["regular", "follow_up"], codes);
            Assert.False(defaults.GetProperty("data")[0].TryGetProperty("queue_priority", out _));

            (await admin.PutAsJsonAsync(
                "/staff/appointment-types",
                new { types = new object[] { new { code = "regular", name = "นัดปกติ" }, new { code = "follow_up", name = "ติดตามผล", online_booking = false }, new { code = "vaccine", name = "ฉีดวัคซีน", duration_minutes = 15 } } },
                Token)).EnsureSuccessStatusCode();

            var changed = await customer.GetFromJsonAsync<JsonElement>("/portal/appointment-types", Token);
            var vaccine = changed.GetProperty("data")[1];

            Assert.Equal(2, changed.GetProperty("data").GetArrayLength());
            Assert.Equal("vaccine", vaccine.GetProperty("code").GetString());
            Assert.Equal(15, vaccine.GetProperty("duration_minutes").GetInt32());
        }
        finally
        {
            (await admin.PutAsJsonAsync("/staff/appointment-types", DefaultTypes, Token)).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task The_portal_list_is_for_customers_only()
    {
        using var staff = Staff(Scopes.Appointment.Read);

        var response = await staff.GetAsync("/portal/appointment-types", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
