using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Integrations;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Integrations;

/// <summary>
/// The outbox: business changes queue their webhook event in the same transaction, a rolled-back
/// change queues nothing, and no event carries health or contact data.
/// </summary>
[Collection(nameof(IntegrationsCollection))]
public class WebhookOutboxTests(IntegrationsFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_rolled_back_change_leaves_no_event_and_a_committed_one_leaves_exactly_one()
    {
        var marker = $"cus_rollback_{Guid.NewGuid():N}";

        using (var scope = fixture.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var outbox = scope.ServiceProvider.GetRequiredService<IWebhookOutbox>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync(_ =>
            {
                outbox.Enqueue(WebhookEventTypes.CustomerCreated, new WebhookPayloads.CustomerData(marker, DateTimeOffset.UtcNow));
                throw new InvalidOperationException("the business change failed after the event was queued");
            }, Token));
        }

        Assert.Empty(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.CustomerCreated, marker, Token));

        using (var scope = fixture.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var outbox = scope.ServiceProvider.GetRequiredService<IWebhookOutbox>();

            await unitOfWork.ExecuteInTransactionAsync(_ =>
            {
                outbox.Enqueue(WebhookEventTypes.CustomerCreated, new WebhookPayloads.CustomerData(marker, DateTimeOffset.UtcNow));
                return Task.CompletedTask;
            }, Token);
        }

        Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.CustomerCreated, marker, Token));
    }

    [Fact]
    public async Task A_request_refused_by_the_business_rules_queues_nothing()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);
        var appointmentId = await BookAsStaffAsync(fixture, clinic, 4, Token);

        // Moving the visit onto a time outside every shift is refused after the appointment was
        // loaded, inside the transaction.
        var refused = await staff.PostAsJsonAsync(
            $"/appointments/{appointmentId}/reschedule",
            new { starts_at = ClinicSeed.SlotAt(24 * 30) },
            Token);

        Assert.False(refused.IsSuccessStatusCode);
        Assert.Empty(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.AppointmentRescheduled, appointmentId, Token));
    }

    [Fact]
    public async Task The_appointment_lifecycle_queues_its_events()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        var appointmentId = await BookAsStaffAsync(fixture, clinic, 4, Token);
        var created = Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.AppointmentCreated, appointmentId, Token));

        var data = Data(created);
        Assert.Equal("booked", data.GetProperty("status").GetString());
        Assert.Equal(clinic.PetId, data.GetProperty("pet_id").GetString());
        Assert.False(data.TryGetProperty("reason", out _));

        (await staff.PostAsJsonAsync($"/appointments/{appointmentId}/reschedule", new { starts_at = ClinicSeed.SlotAt(6) }, Token)).EnsureSuccessStatusCode();
        var moved = Data(Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.AppointmentRescheduled, appointmentId, Token)));
        Assert.True(moved.TryGetProperty("previous_starts_at", out _));

        (await staff.PostAsJsonAsync($"/appointments/{appointmentId}/cancel", new { reason = "เจ้าของป่วยเป็นไข้หวัด" }, Token)).EnsureSuccessStatusCode();
        var cancelled = Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.AppointmentCancelled, appointmentId, Token));
        Assert.DoesNotContain("ไข้หวัด", cancelled.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Check_in_call_and_close_queue_their_events_without_clinical_content()
    {
        var open = await ConsultationScenario.OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.AppointmentCheckedIn, open.AppointmentId, Token));

        await ConsultationScenario.DiagnoseAsync(vet, open.ConsultationId, Token, description: "โรคไตเรื้อรังระยะที่สาม");
        var closed = await ConsultationScenario.CloseAsync(vet, open.ConsultationId, Token);
        var recordId = closed.GetProperty("record").GetProperty("id").GetString()!;

        var consultation = Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.ConsultationClosed, open.ConsultationId, Token));
        var record = Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.RecordSigned, recordId, Token));

        foreach (var body in new[] { consultation.Body, record.Body })
        {
            Assert.DoesNotContain("ไต", body, StringComparison.Ordinal);
            Assert.DoesNotContain("SKIN-01", body, StringComparison.Ordinal);
            Assert.DoesNotContain("diagnos", body, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal(open.ConsultationId, Data(record).GetProperty("consultation_id").GetString());
    }

    [Fact]
    public async Task Calling_a_ticket_queues_queue_ticket_called()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var appointmentId = await BookAsStaffAsync(fixture, clinic, 3, Token);
        using var vet = fixture.Vet(clinic);
        var ticketId = await ConsultationScenario.CheckInAsync(vet, appointmentId, Token);

        (await vet.PostAsJsonAsync($"/staff/queue/{ticketId}/call", new { }, Token)).EnsureSuccessStatusCode();

        var called = Data(Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.QueueTicketCalled, ticketId, Token)));
        Assert.Equal(appointmentId, called.GetProperty("appointment_id").GetString());
    }

    [Fact]
    public async Task Billing_queues_issued_paid_and_refunded_without_the_bill_lines()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var cashier = fixture.Cashier(clinic);

        var invoiceId = await BillingScenario.RaiseIdAsync(cashier, clinic, Token);
        (await BillingScenario.PayAsync(cashier, invoiceId, 146_000, Token)).EnsureSuccessStatusCode();

        using (var refund = Idempotent($"/staff/invoices/{invoiceId}/refunds", new { amount_satang = 10_000, reason = "คิดค่าทำแผลเกิน" }))
        {
            (await cashier.SendAsync(refund, Token)).EnsureSuccessStatusCode();
        }

        var issued = Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.InvoiceIssued, invoiceId, Token));
        Assert.Equal(146_000, Data(issued).GetProperty("total_satang").GetInt64());
        Assert.DoesNotContain("ทำแผล", issued.Body, StringComparison.Ordinal);

        var paid = Data(Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.PaymentSucceeded, invoiceId, Token)));
        Assert.Equal("cash", paid.GetProperty("method").GetString());
        Assert.Equal(0, paid.GetProperty("balance_satang").GetInt64());

        var refunded = Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.PaymentRefunded, invoiceId, Token));
        Assert.Equal(10_000, Data(refunded).GetProperty("amount_satang").GetInt64());
        Assert.DoesNotContain("ทำแผล", refunded.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Customer_and_pet_registration_queue_their_events()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write, Scopes.Patient.Write);

        var customerId = await IntegrationsScenario.CreateCustomerAsync(counter, Token);
        Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.CustomerCreated, customerId, Token));

        var response = await counter.PostAsJsonAsync("/staff/pets", new { name = "โมจิ", species = "แมว", primary_customer_id = customerId }, Token);
        response.EnsureSuccessStatusCode();
        var petId = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!;

        var pet = Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.PetCreated, petId, Token));
        Assert.Equal("แมว", Data(pet).GetProperty("species").GetString());
        Assert.DoesNotContain("โมจิ", pet.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Low_stock_is_queued_once_when_an_item_crosses_its_reorder_level()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var store = fixture.Storekeeper(clinic);

        var item = await InventoryScenario.ReceiveNewAsync(store, clinic, InventoryScenario.UniqueName("Amoxicillin"), 10, Token, reorderLevel: 5);
        var itemId = InventoryScenario.ItemId(item);

        await Issue(4);
        Assert.Empty(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.InventoryLowStock, itemId, Token));

        await Issue(2);
        var low = Data(Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.InventoryLowStock, itemId, Token)));
        Assert.Equal(4, low.GetProperty("available").GetInt32());
        Assert.Equal(5, low.GetProperty("reorder_level").GetInt32());

        await Issue(1);
        Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.InventoryLowStock, itemId, Token));

        async Task Issue(int quantity) =>
            await InventoryScenario.OkAsync(
                await InventoryScenario.MoveAsync(store, new { type = "issue", branch_id = clinic.BranchId, item_id = itemId, quantity }, Token),
                Token);
    }

    [Fact]
    public async Task Every_body_is_the_documented_envelope()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = fixture.Integrator(clinic, Scopes.Customer.Write);
        var customerId = await IntegrationsScenario.CreateCustomerAsync(counter, Token);

        var stored = Assert.Single(await IntegrationsScenario.EventsAboutAsync(fixture, WebhookEventTypes.CustomerCreated, customerId, Token));
        var body = JsonDocument.Parse(stored.Body).RootElement;

        Assert.Equal(["id", "type", "occurred_at", "data"], body.EnumerateObject().Select(property => property.Name));
        Assert.Equal(stored.Id, body.GetProperty("id").GetString());
        Assert.Null(stored.DispatchedAt);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.True(await db.Set<WebhookEvent>().AnyAsync(row => row.Id == stored.Id, Token));
    }

    private static JsonElement Data(WebhookEvent stored) =>
        JsonDocument.Parse(stored.Body).RootElement.GetProperty("data").Clone();
}
