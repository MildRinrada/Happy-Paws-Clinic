using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>
/// Turning a slot into an appointment: the customer path, the counter path, and the guarantees
/// that hold when two people want the same time.
/// </summary>
[Collection(nameof(ApiCollection))]
public class BookingTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Booking_requires_an_idempotency_key()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var customer = fixture.CustomerOf(clinic);

        var response = await customer.PostAsJsonAsync(
            "/portal/appointments", Booking(clinic, ClinicSeed.SlotAt(5)), Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("idempotency_key_required", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_customer_books_their_own_pet_by_redeeming_a_hold()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var startsAt = ClinicSeed.SlotAt(6);

        using var customer = fixture.CustomerOf(clinic);

        var held = await customer.PostAsJsonAsync(
            "/booking/holds",
            new { service_id = clinic.ServiceId, starts_at = startsAt, branch_id = clinic.BranchId },
            Token);

        var holdId = (await held.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString();

        using var request = Idempotent("/portal/appointments", new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            starts_at = startsAt,
            branch_id = clinic.BranchId,
            hold_id = holdId,
            reason = "ไอและซึม 2 วัน",
        });

        var response = await customer.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var appointment = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("booked", appointment.GetProperty("status").GetString());
        Assert.Equal(clinic.PetId, appointment.GetProperty("pet_id").GetString());
        Assert.Equal(clinic.StaffId, appointment.GetProperty("staff_id").GetString());
        Assert.Equal("ไอและซึม 2 วัน", appointment.GetProperty("reason").GetString());
        Assert.StartsWith("apt_", appointment.GetProperty("id").GetString());

        // The version a client echoes back in If-Match, in the body and as an ETag.
        Assert.True(appointment.GetProperty("version").GetUInt32() > 0);
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task Booking_a_taken_slot_answers_slot_unavailable_with_alternatives()
    {
        // This is the contract's own worked example of the error envelope.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var startsAt = ClinicSeed.SlotAt(7);

        using var staff = fixture.StaffOf(clinic);
        using var first = Idempotent("/staff/appointments", Booking(clinic, startsAt));
        Assert.Equal(HttpStatusCode.Created, (await staff.SendAsync(first, Token)).StatusCode);

        using var customer = fixture.CustomerOf(clinic);
        using var second = Idempotent("/portal/appointments", Booking(clinic, startsAt));

        var response = await customer.SendAsync(second, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

        Assert.Equal("slot_unavailable", error.GetProperty("code").GetString());
        Assert.Equal("starts_at", error.GetProperty("field").GetString());
        Assert.Equal("Slot already booked", error.GetProperty("message_en").GetString());

        // The suggestions are what turn the refusal into the customer's next step.
        var suggested = error.GetProperty("details").GetProperty("suggested").EnumerateArray().ToList();
        Assert.NotEmpty(suggested);
        Assert.All(suggested, time => Assert.Matches(@"^\d{2}:\d{2}$", time.GetString()));
    }

    [Fact]
    public async Task A_customer_cannot_book_a_pet_they_do_not_care_for()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (_, otherPetId) = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        using var customer = fixture.CustomerOf(clinic);
        using var request = Idempotent("/portal/appointments", new
        {
            pet_id = otherPetId,
            service_id = clinic.ServiceId,
            starts_at = ClinicSeed.SlotAt(8),
            branch_id = clinic.BranchId,
        });

        var response = await customer.SendAsync(request, Token);

        // 404, not 403 — a "forbidden" here would confirm the pet id is real.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_customer_cannot_redeem_someone_elses_hold()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var startsAt = ClinicSeed.SlotAt(9);

        using var owner = fixture.CustomerOf(clinic);
        var held = await owner.PostAsJsonAsync(
            "/booking/holds",
            new { service_id = clinic.ServiceId, starts_at = startsAt, branch_id = clinic.BranchId },
            Token);

        var holdId = (await held.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString();

        var (otherCustomerId, otherPetId) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var stranger = fixture.Authenticated(TestTokens.Customer(fixture, "usr_stranger", otherCustomerId));

        using var request = Idempotent("/portal/appointments", new
        {
            pet_id = otherPetId,
            service_id = clinic.ServiceId,
            starts_at = startsAt,
            branch_id = clinic.BranchId,
            hold_id = holdId,
        });

        var response = await stranger.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("hold_not_yours", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Staff_may_book_inside_the_lead_time_and_customers_may_not()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var soon = DateTimeOffset.UtcNow.AddMinutes(20);

        using var customer = fixture.CustomerOf(clinic);
        using var tooSoon = Idempotent("/portal/appointments", Booking(clinic, soon));

        var refused = await customer.SendAsync(tooSoon, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);

        var error = (await refused.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("slot_unavailable", error.GetProperty("code").GetString());

        // The counter takes the same slot for the pet already standing there.
        using var staff = fixture.StaffOf(clinic);
        using var walkIn = Idempotent("/staff/appointments", new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            starts_at = soon,
            branch_id = clinic.BranchId,
            walk_in = true,
        });

        var accepted = await staff.SendAsync(walkIn, Token);

        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.True((await accepted.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("walk_in").GetBoolean());
    }

    [Fact]
    public async Task A_retried_booking_creates_one_appointment_not_two()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var body = Booking(clinic, ClinicSeed.SlotAt(10));
        var key = Guid.NewGuid().ToString();

        using var customer = fixture.CustomerOf(clinic);
        using var first = Idempotent("/portal/appointments", body, key);
        using var retry = Idempotent("/portal/appointments", body, key);

        var created = await customer.SendAsync(first, Token);
        var replayed = await customer.SendAsync(retry, Token);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replayed.StatusCode);

        var firstId = (await created.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString();
        var secondId = (await replayed.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString();

        // Same appointment, not a second booking of the same slot.
        Assert.Equal(firstId, secondId);
        Assert.Equal("true", replayed.Headers.GetValues("Idempotent-Replay").Single());
    }

    [Fact]
    public async Task A_start_time_sent_with_the_contracts_bangkok_offset_is_accepted()
    {
        // The contract's own example is 2026-08-22T10:30:00+07:00, and Npgsql refuses to write a
        // DateTimeOffset whose offset is not zero. Without the UTC conversion in the mapping, every
        // booking made from Thailand fails on insert — so the offset is sent literally here rather
        // than normalised by the test client.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var utc = ClinicSeed.SlotAt(13);
        var bangkok = utc.ToOffset(TimeSpan.FromHours(7));

        using var staff = fixture.StaffOf(clinic);
        using var request = Idempotent("/staff/appointments", new
        {
            pet_id = clinic.PetId,
            service_id = clinic.ServiceId,
            starts_at = bangkok.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            branch_id = clinic.BranchId,
        });

        var response = await staff.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Same instant, whichever offset it arrived in.
        var appointment = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(utc, appointment.GetProperty("starts_at").GetDateTimeOffset());
    }

    [Fact]
    public async Task Two_simultaneous_bookings_of_one_slot_cannot_both_succeed()
    {
        // The in-transaction re-check is necessary but not sufficient: under READ COMMITTED both
        // requests read a free calendar and neither sees the other's uncommitted row. The database
        // exclusion constraint is what actually decides this, and this test is what proves it is
        // there — and that the winner is told it won.
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var startsAt = ClinicSeed.SlotAt(14);

        var attempts = Enumerable.Range(0, 4).Select(async _ =>
        {
            using var staff = fixture.StaffOf(clinic);
            using var request = Idempotent("/staff/appointments", Booking(clinic, startsAt, clinic.StaffId));

            var response = await staff.SendAsync(request, Token);

            return (response.StatusCode, Body: await response.Content.ReadAsStringAsync(Token));
        });

        var results = await Task.WhenAll(attempts);
        var landed = await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token);

        var report = string.Join("\n", results.Select(r => $"{(int)r.StatusCode} {r.Body}"))
            + $"\nrows in database: {landed.Count}";

        // Both halves matter. One 201 alone would not prove the constraint held, and one row alone
        // would not prove the winner was told it won.
        Assert.True(
            results.Count(r => r.StatusCode == HttpStatusCode.Created) == 1,
            $"exactly one booking should win:\n{report}");

        Assert.True(landed.Count == 1, $"exactly one appointment should exist:\n{report}");

        Assert.All(
            results.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode));
    }
}
