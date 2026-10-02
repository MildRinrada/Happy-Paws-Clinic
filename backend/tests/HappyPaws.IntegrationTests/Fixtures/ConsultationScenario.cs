using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// A patient in the consulting room: booked, checked in, called, and a case open on them.
/// <para>
/// The whole path is driven through the API rather than seeded, because that path is the thing
/// under test — the queue ticket and the appointment have to move with the consultation, and a
/// seeded shortcut would prove nothing about whether they do.
/// </para>
/// </summary>
public sealed record OpenCase(SeededClinic Clinic, string AppointmentId, string TicketId, string ConsultationId);

public static class ConsultationScenario
{
    /// <summary>Every scope the consultation flow touches, for the tests that are not about scopes.</summary>
    public static readonly string[] ClinicalScopes =
    [
        Scopes.Appointment.Read,
        Scopes.Appointment.Write,
        Scopes.Appointment.CheckIn,
        Scopes.Queue.Read,
        Scopes.Queue.Write,
        Scopes.Queue.Call,
        Scopes.Consult.Read,
        Scopes.Consult.Write,
        Scopes.Vitals.Write,
        Scopes.Prescription.Write,
        Scopes.Record.Read,
        Scopes.Record.Write,
        Scopes.Record.Sign,
        Scopes.Record.Amend,
        Scopes.Record.Audit,
        Scopes.Patient.Write,
        Scopes.Audit.Read,
    ];

    public static HttpClient Vet(this ApiFixture fixture, SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? ClinicalScopes : scopes,
            [clinic.BranchId]));

    /// <summary>Books, checks in and opens a case — the contract's own flow, through the API.</summary>
    public static async Task<OpenCase> OpenCaseAsync(
        ApiFixture fixture,
        CancellationToken cancellationToken,
        int hoursFromNow = 3)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: cancellationToken);
        var appointmentId = await BookAsStaffAsync(fixture, clinic, hoursFromNow, cancellationToken);

        using var vet = fixture.Vet(clinic);

        var ticketId = await CheckInAsync(vet, appointmentId, cancellationToken);
        var consultationId = await OpenAsync(vet, appointmentId, cancellationToken);

        return new OpenCase(clinic, appointmentId, ticketId, consultationId);
    }

    public static async Task<string> CheckInAsync(
        HttpClient staff,
        string appointmentId,
        CancellationToken cancellationToken)
    {
        using var request = Idempotent($"/appointments/{appointmentId}/check-in");

        var response = await staff.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return body.GetProperty("ticket").GetProperty("id").GetString()!;
    }

    public static async Task<string> OpenAsync(
        HttpClient staff,
        string appointmentId,
        CancellationToken cancellationToken)
    {
        using var request = Idempotent("/staff/consultations", new { appointment_id = appointmentId });

        var response = await staff.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return body.GetProperty("id").GetString()!;
    }

    /// <summary>The minimum a case needs before it can be closed: one diagnosis.</summary>
    public static async Task DiagnoseAsync(
        HttpClient staff,
        string consultationId,
        CancellationToken cancellationToken,
        string description = "ผิวหนังอักเสบ")
    {
        var response = await staff.PostAsJsonAsync(
            $"/staff/consultations/{consultationId}/diagnoses",
            new { description, is_primary = true, code = "SKIN-01" },
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>Closes a case and returns the whole result envelope.</summary>
    public static async Task<JsonElement> CloseAsync(
        HttpClient staff,
        string consultationId,
        CancellationToken cancellationToken,
        object? followUp = null)
    {
        using var request = Idempotent(
            $"/staff/consultations/{consultationId}/close",
            followUp is null ? new { } : new { follow_up = followUp });

        var response = await staff.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }
}
