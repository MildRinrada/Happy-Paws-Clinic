using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>"แผนหลายครั้ง · ผูกกับนัดและการเตือน".</summary>
[Collection(nameof(ApiCollection))]
public class TreatmentPlanTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient VetOf(SeededClinic clinic) =>
        fixture.StaffOf(clinic, Scopes.Consult.Write, Scopes.Appointment.Read, Scopes.Appointment.Write);

    [Fact]
    public async Task A_plan_books_every_session_and_each_session_names_its_appointment()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await AdvancedBookingSeed.NextDayShiftAsync(fixture, clinic, Token);
        using var vet = VetOf(clinic);

        var response = await vet.SendAsync(
            Idempotent($"/staff/pets/{clinic.PetId}/treatment-plans", new
            {
                title = "ฉีดยาต่อเนื่อง 2 ครั้ง",
                service_id = clinic.ServiceId,
                branch_id = clinic.BranchId,
                first_starts_at = ClinicSeed.SlotAt(4),
                sessions = 2,
                interval_days = 1,
            }),
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var sessions = plan.GetProperty("sessions").EnumerateArray().ToList();

        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, session => Assert.StartsWith("apt_", session.GetProperty("appointment_id").GetString()));

        var booked = await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token);
        Assert.All(booked, appointment => Assert.Equal(plan.GetProperty("series_id").GetString(), appointment.SeriesId));
    }

    [Fact]
    public async Task A_plan_made_without_booking_has_dates_and_no_appointments()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = VetOf(clinic);

        var response = await vet.SendAsync(
            Idempotent($"/staff/pets/{clinic.PetId}/treatment-plans", new
            {
                title = "คอร์สกายภาพ",
                service_id = clinic.ServiceId,
                first_starts_at = ClinicSeed.SlotAt(30),
                rrule = "FREQ=WEEKLY;COUNT=4",
                book = false,
            }),
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(4, plan.GetProperty("sessions").GetArrayLength());
        Assert.False(plan.TryGetProperty("series_id", out _));
        Assert.Empty(await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token));
    }

    [Fact]
    public async Task A_plan_needs_a_way_to_know_how_many_sessions()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = VetOf(clinic);

        var response = await vet.SendAsync(
            Idempotent($"/staff/pets/{clinic.PetId}/treatment-plans", new
            {
                title = "ไม่รู้กี่ครั้ง",
                service_id = clinic.ServiceId,
                first_starts_at = ClinicSeed.SlotAt(4),
            }),
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
