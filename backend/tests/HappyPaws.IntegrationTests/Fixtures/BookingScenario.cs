using System.Net.Http.Json;
using HappyPaws.Domain.Access;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// The request-shaping the appointment tests all need: a client for each audience, and a POST that
/// carries the <c>Idempotency-Key</c> the contract requires.
/// <para>
/// Shared rather than repeated so a test reads as the scenario it is testing, and so a change to
/// how a request is formed does not have to be made in four files.
/// </para>
/// </summary>
public static class BookingScenario
{
    public static HttpClient CustomerOf(this ApiFixture fixture, SeededClinic clinic) =>
        fixture.Authenticated(TestTokens.Customer(fixture, clinic.CustomerUserId, clinic.CustomerId));

    /// <summary>Defaults to the scopes the appointment endpoints need; pass others to test refusals.</summary>
    public static HttpClient StaffOf(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0
                ? [Scopes.Appointment.Read, Scopes.Appointment.Write, Scopes.Appointment.CheckIn]
                : scopes,
            [clinic.BranchId]));

    /// <summary>A POST carrying an idempotency key. Pass <paramref name="key"/> to simulate a retry.</summary>
    public static HttpRequestMessage Idempotent(string path, object? body = null, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());

        return request;
    }

    /// <summary>The body of a straightforward booking, staff or portal.</summary>
    public static object Booking(SeededClinic clinic, DateTimeOffset startsAt, string? staffId = null) => new
    {
        pet_id = clinic.PetId,
        service_id = clinic.ServiceId,
        starts_at = startsAt,
        staff_id = staffId,
        branch_id = clinic.BranchId,
    };

    /// <summary>Books through <c>POST /staff/appointments</c> and returns the new appointment's id.</summary>
    public static async Task<string> BookAsStaffAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        int hoursFromNow,
        CancellationToken cancellationToken)
    {
        using var staff = fixture.StaffOf(clinic);
        using var request = Idempotent("/staff/appointments", Booking(clinic, ClinicSeed.SlotAt(hoursFromNow)));

        var response = await staff.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken);

        return body.GetProperty("id").GetString()!;
    }
}
