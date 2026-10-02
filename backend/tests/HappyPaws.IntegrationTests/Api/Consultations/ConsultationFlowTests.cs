using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Consultations;

/// <summary>
/// The contract's second worked example, end to end:
/// <c>POST /staff/consultations</c> → vitals → examination → diagnoses → prescriptions → close,
/// and what that does to the queue and the calendar on the way past.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ConsultationFlowTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Opening_a_case_takes_the_patient_into_the_room_and_out_of_the_waiting_list()
    {
        var open = await OpenCaseAsync(fixture, Token);

        var ticket = await QueueSeed.ReloadAsync(fixture, open.TicketId, Token);
        var appointment = await ClinicSeed.ReloadAppointmentAsync(fixture, open.AppointmentId, Token);

        // Three things move together. A case with the ticket left waiting means the board shows a
        // patient in the waiting room who is in front of the vet.
        Assert.Equal(Domain.Queueing.QueueTicketStatus.InRoom, ticket!.Status);
        Assert.Equal(Domain.Appointments.AppointmentStatus.InConsultation, appointment!.Status);
        Assert.StartsWith("con_", open.ConsultationId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Opening_the_same_visit_twice_returns_the_case_that_already_exists()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        var again = await OpenAsync(vet, open.AppointmentId, Token);

        // The usual reason a vet presses again is that the first response never arrived. A 409
        // there would strand them on a case they cannot see.
        Assert.Equal(open.ConsultationId, again);
    }

    [Fact]
    public async Task A_full_case_records_every_part_and_closes_into_a_signed_record()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await Post(vet, open, "vitals", new { weight_kg = 8.4, temperature_c = 39.2, heart_rate_bpm = 120 });
        await Post(vet, open, "examination", new
        {
            findings = new object[]
            {
                new { body_system = "ผิวหนัง", is_normal = false, note = "ผื่นแดงบริเวณท้อง" },
                new { body_system = "ช่องปาก", is_normal = true },
            },
        });
        await DiagnoseAsync(vet, open.ConsultationId, Token);
        await Post(vet, open, "treatments", new { name = "ทำแผล", quantity = 1, unit_price_satang = 46_000 });

        var result = await CloseAsync(vet, open.ConsultationId, Token);

        Assert.Equal("closed", result.GetProperty("consultation").GetProperty("status").GetString());
        Assert.Equal(46_000, result.GetProperty("charges_satang").GetInt64());

        var record = result.GetProperty("record");
        Assert.Equal("signed", record.GetProperty("status").GetString());
        Assert.Equal(open.Clinic.StaffId, record.GetProperty("signed_by_staff_id").GetString());
        Assert.True(record.GetProperty("integrity_verified").GetBoolean());

        // The snapshot is the document, not a pointer to one: everything entered during the visit
        // is inside it.
        var content = record.GetProperty("content");
        Assert.Equal("ผิวหนังอักเสบ", content.GetProperty("diagnoses")[0].GetProperty("description").GetString());
        Assert.Equal(8.4m, content.GetProperty("vitals")[0].GetProperty("weight_kg").GetDecimal());
        Assert.Equal(2, content.GetProperty("examination").GetArrayLength());
    }

    [Fact]
    public async Task Closing_releases_the_room_and_completes_the_visit()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await DiagnoseAsync(vet, open.ConsultationId, Token);
        await CloseAsync(vet, open.ConsultationId, Token);

        var ticket = await QueueSeed.ReloadAsync(fixture, open.TicketId, Token);
        var appointment = await ClinicSeed.ReloadAppointmentAsync(fixture, open.AppointmentId, Token);

        Assert.Equal(Domain.Queueing.QueueTicketStatus.Served, ticket!.Status);
        Assert.Equal(Domain.Appointments.AppointmentStatus.Completed, appointment!.Status);
    }

    [Fact]
    public async Task Closing_can_book_the_follow_up_in_the_same_breath()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await DiagnoseAsync(vet, open.ConsultationId, Token);

        var followUpAt = ClinicSeed.SlotAt(9);
        var result = await CloseAsync(
            vet, open.ConsultationId, Token, new { starts_at = followUpAt });

        var followUp = result.GetProperty("follow_up");
        Assert.Equal("booked", followUp.GetProperty("status").GetString());
        Assert.Equal(followUpAt, followUp.GetProperty("starts_at").GetDateTimeOffset());
        // Repeats this visit's service when the caller did not name one.
        Assert.Equal(open.Clinic.ServiceId, followUp.GetProperty("service_id").GetString());
    }

    [Fact]
    public async Task A_case_with_no_diagnosis_cannot_be_closed()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        using var request = BookingScenario.Idempotent($"/staff/consultations/{open.ConsultationId}/close", new { });
        var response = await vet.SendAsync(request, Token);

        // A signed record with no diagnosis is not a clinical document.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_closed_case_answers_423_and_points_at_the_amendment_path()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await DiagnoseAsync(vet, open.ConsultationId, Token);
        await CloseAsync(vet, open.ConsultationId, Token);

        var response = await vet.PatchAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}",
            new { assessment = "คิดใหม่" },
            Token);

        Assert.Equal(HttpStatusCode.Locked, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("record_locked", error.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Autosave_keeps_what_it_was_not_sent()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await vet.PatchAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}", new { subjective = "ซึม ไม่กินอาหาร" }, Token);

        var second = await vet.PatchAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}", new { assessment = "สงสัยติดเชื้อ" }, Token);

        var consultation = await second.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("ซึม ไม่กินอาหาร", consultation.GetProperty("subjective").GetString());
        Assert.Equal("สงสัยติดเชื้อ", consultation.GetProperty("assessment").GetString());
    }

    private static async Task Post(HttpClient vet, OpenCase open, string part, object body)
    {
        HttpResponseMessage response;

        if (part is "treatments" or "prescriptions")
        {
            using var request = BookingScenario.Idempotent(
                $"/staff/consultations/{open.ConsultationId}/{part}", body);
            response = await vet.SendAsync(request, Token);
        }
        else
        {
            response = await vet.PostAsJsonAsync($"/staff/consultations/{open.ConsultationId}/{part}", body, Token);
        }

        response.EnsureSuccessStatusCode();
        response.Dispose();
    }
}
