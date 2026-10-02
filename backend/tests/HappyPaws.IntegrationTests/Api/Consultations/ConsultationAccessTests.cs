using System.Net;
using System.Net.Http.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Consultations;

/// <summary>
/// Who may do what inside the consulting room.
/// <para>
/// The contract's default roles draw real lines here — <c>vet_assistant</c> holds
/// <c>vitals:write</c> and nothing else clinical, and <c>record:sign</c> is withheld from everyone
/// but the vet. These tests are the proof that those lines are enforced at the boundary rather
/// than only drawn in a menu.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class ConsultationAccessTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_assistant_can_take_vitals_and_write_nothing_else()
    {
        var open = await OpenCaseAsync(fixture, Token);

        // Exactly what the contract grants vet_assistant: queue:read, patient:read, vitals:write.
        using var assistant = fixture.Vet(open.Clinic, Scopes.Queue.Read, Scopes.Patient.Read, Scopes.Vitals.Write);

        var vitals = await assistant.PostAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}/vitals", new { weight_kg = 8.4 }, Token);
        var diagnosis = await assistant.PostAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}/diagnoses", new { description = "เดาเอา" }, Token);

        Assert.Equal(HttpStatusCode.OK, vitals.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, diagnosis.StatusCode);
    }

    [Fact]
    public async Task Writing_on_a_case_and_signing_the_record_are_different_powers()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await DiagnoseAsync(vet, open.ConsultationId, Token);

        using var writer = fixture.Vet(open.Clinic, Scopes.Consult.Write);
        using var request = BookingScenario.Idempotent($"/staff/consultations/{open.ConsultationId}/close", new { });

        var response = await writer.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Prescribing_needs_its_own_scope()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var writer = fixture.Vet(open.Clinic, Scopes.Consult.Write);

        using var request = BookingScenario.Idempotent(
            $"/staff/consultations/{open.ConsultationId}/prescriptions",
            new
            {
                drug_name = "Meloxicam",
                dose = "0.5 ml",
                frequency = "วันละครั้ง",
                duration_days = 3,
                quantity = 3,
            });

        var response = await writer.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_case_at_another_branch_is_out_of_reach()
    {
        var open = await OpenCaseAsync(fixture, Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var outsider = fixture.Vet(elsewhere, Scopes.Consult.Write);

        var response = await outsider.PatchAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}", new { assessment = "x" }, Token);

        // Holding consult:write answers "may this person write on cases", not "may this person
        // write on cases at a site they do not work at".
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_cannot_reach_the_consulting_room_at_all()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var owner = fixture.CustomerOf(open.Clinic);

        var response = await owner.PatchAsJsonAsync(
            $"/staff/consultations/{open.ConsultationId}", new { assessment = "x" }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Opening_a_case_needs_either_an_appointment_or_a_ticket()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.Vet(clinic);

        using var request = BookingScenario.Idempotent("/staff/consultations", new { });
        var response = await vet.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_ticket_that_belongs_to_a_different_appointment_is_refused()
    {
        var first = await OpenCaseAsync(fixture, Token);
        var secondAppointment = await BookingScenario.BookAsStaffAsync(fixture, first.Clinic, 7, Token);

        using var vet = fixture.Vet(first.Clinic);
        using var request = BookingScenario.Idempotent(
            "/staff/consultations",
            new { appointment_id = secondAppointment, queue_ticket_id = first.TicketId });

        var response = await vet.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
