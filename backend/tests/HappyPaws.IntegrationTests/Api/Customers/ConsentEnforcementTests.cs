using System.Net.Http.Json;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Customers;

/// <summary>
/// The part that makes consent real: a message the clinic does not send.
/// <para>
/// These tests go through the whole stack — a queue call, the dispatcher, the consent gate, the
/// channel — because that is the only place the rule can be shown to hold. The channel is the only
/// thing substituted, so the gate under test is the production one.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class ConsentEnforcementTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private IReadOnlyList<NotificationRequest> Delivered(SeededClinic clinic) =>
        fixture.Notifications.SentTo(clinic.CustomerId);

    /// <summary>Books, checks in and calls the patient — the flow that pushes to the owner.</summary>
    private async Task CallPatientAsync(SeededClinic clinic)
    {
        var appointmentId = await BookingScenario.BookAsStaffAsync(fixture, clinic, 3, Token);

        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            [Scopes.Appointment.CheckIn, Scopes.Queue.Call],
            [clinic.BranchId]));

        var ticketId = await CheckInAsync(staff, appointmentId, Token);

        var called = await staff.PostAsJsonAsync($"/staff/queue/{ticketId}/call", new { }, Token);
        called.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_queue_call_reaches_an_owner_who_has_not_opted_out()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        await CallPatientAsync(clinic);

        // Care messaging is on by default: the owner gave the clinic a number in order to be
        // contacted about their animal.
        Assert.Contains(Delivered(clinic), sent => sent.TemplateKey == NotificationTemplates.QueueCalled);
    }

    [Fact]
    public async Task Withdrawing_service_messages_stops_the_queue_call_from_being_sent()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var owner = fixture.CustomerOf(clinic);
        var withdrawn = await owner.PutAsJsonAsync(
            "/portal/consents", new { consents = new { service_messages = false } }, Token);
        withdrawn.EnsureSuccessStatusCode();

        await CallPatientAsync(clinic);

        // The toggle in the portal is binding, not decorative — and the queue still worked: the
        // patient was called, the board moved, only the push was suppressed.
        Assert.Empty(Delivered(clinic));
    }

    [Fact]
    public async Task A_suppressed_message_does_not_fail_the_action_that_triggered_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var owner = fixture.CustomerOf(clinic);
        await owner.PutAsJsonAsync(
            "/portal/consents", new { consents = new { service_messages = false } }, Token);

        // CallPatientAsync throws on any non-success status, so reaching the end is the assertion:
        // a consent decision must never turn a working queue into an error at the counter.
        await CallPatientAsync(clinic);

        var board = await fixture
            .Authenticated(TestTokens.Staff(
                fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Queue.Read], [clinic.BranchId]))
            .GetFromJsonAsync<System.Text.Json.JsonElement>($"/staff/queue?branch={clinic.BranchId}", Token);

        Assert.Equal("called", board.GetProperty("tickets")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Opting_back_in_starts_the_messages_again()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var owner = fixture.CustomerOf(clinic);

        await owner.PutAsJsonAsync(
            "/portal/consents", new { consents = new { service_messages = false } }, Token);
        await owner.PutAsJsonAsync(
            "/portal/consents", new { consents = new { service_messages = true } }, Token);

        await CallPatientAsync(clinic);

        Assert.Contains(Delivered(clinic), sent => sent.TemplateKey == NotificationTemplates.QueueCalled);
    }
}
