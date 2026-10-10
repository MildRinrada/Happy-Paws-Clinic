using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Patients;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>
/// Booking without signing up first (docs/contract-extensions.md § 27): hold a time and text a
/// code, prove the number, become a signed-in customer, then book through the ordinary
/// <c>POST /portal/appointments</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class GuestBookingTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string NewPhone() => $"06{Random.Shared.Next(10_000_000, 99_999_999)}";

    private static object StartBody(SeededClinic clinic, string phone, int hoursFromNow) => new
    {
        phone,
        service_id = clinic.ServiceId,
        branch_id = clinic.BranchId,
        starts_at = ClinicSeed.SlotAt(hoursFromNow),
    };

    private static object VerifyBody(string holdId, string phone, string code, bool marketing = false) => new
    {
        hold_id = holdId,
        phone,
        code,
        display_name = "คุณผู้มาใหม่ จองเอง",
        accept_privacy_notice = true,
        policy_version = "2026-10",
        marketing_consent = marketing,
    };

    /// <summary>Starts a guest booking and answers the hold the visitor now has.</summary>
    private async Task<JsonElement> StartAsync(HttpClient client, SeededClinic clinic, string phone, int hoursFromNow)
    {
        var response = await client.PostAsJsonAsync("/booking/guest/start", StartBody(clinic, phone, hoursFromNow), Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("hold");
    }

    private string CodeFor(string phone) =>
        fixture.SignIn.CodeFor(phone) ?? throw new InvalidOperationException("no code was sent");

    private async Task<JsonElement> VerifyAsync(HttpClient client, string holdId, string phone, bool marketing = false)
    {
        var response = await client.PostAsJsonAsync("/booking/guest/verify", VerifyBody(holdId, phone, CodeFor(phone), marketing), Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("hp_refresh", string.Join(";", response.Headers.GetValues("Set-Cookie")));

        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static void SignIn(HttpClient client, JsonElement verified) =>
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", verified.GetProperty("access_token").GetString());

    private static object BookBody(JsonElement hold, object pet) =>
        pet switch
        {
            string petId => new
            {
                pet_id = petId,
                service_id = hold.GetProperty("service_id").GetString(),
                branch_id = hold.GetProperty("branch_id").GetString(),
                staff_id = hold.GetProperty("staff_id").GetString(),
                starts_at = hold.GetProperty("starts_at").GetDateTimeOffset(),
                hold_id = hold.GetProperty("id").GetString(),
            },
            _ => new
            {
                new_pet = pet,
                service_id = hold.GetProperty("service_id").GetString(),
                branch_id = hold.GetProperty("branch_id").GetString(),
                staff_id = hold.GetProperty("staff_id").GetString(),
                starts_at = hold.GetProperty("starts_at").GetDateTimeOffset(),
                hold_id = hold.GetProperty("id").GetString(),
            },
        };

    private async Task<string> CustomerPhoneAsync(string customerId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<Customer>().Where(customer => customer.Id == customerId).Select(customer => customer.Phone).SingleAsync(Token);
    }

    private static async Task<JsonElement> ErrorOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    [Fact]
    public async Task A_new_number_becomes_a_signed_in_customer_and_books_a_new_pet()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();

        var hold = await StartAsync(client, clinic, phone, 5);
        var verified = await VerifyAsync(client, hold.GetProperty("id").GetString()!, phone);

        Assert.True(verified.GetProperty("account_created").GetBoolean());
        Assert.Empty(verified.GetProperty("pets").EnumerateArray());

        var capabilities = verified.GetProperty("capabilities");
        var customerId = capabilities.GetProperty("customer_id").GetString()!;
        Assert.Contains(capabilities.GetProperty("scopes").EnumerateArray(), scope => scope.GetString() == Scopes.Portal);

        SignIn(client, verified);
        using var book = Idempotent("/portal/appointments", BookBody(hold, new { name = "ถั่วเหลือง", species = "แมว" }));
        var booked = await client.SendAsync(book, Token);

        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var appointment = await booked.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(customerId, appointment.GetProperty("customer_id").GetString());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var pet = await db.Set<Pet>().AsNoTracking().SingleAsync(item => item.Id == appointment.GetProperty("pet_id").GetString(), Token);
        Assert.Equal("ถั่วเหลือง", pet.Name);
        Assert.Equal(customerId, pet.PrimaryCustomerId);
        Assert.True(await db.Set<PetOwnership>().AnyAsync(link => link.PetId == pet.Id && link.CustomerId == customerId, Token));

        var customer = await db.Set<Customer>().AsNoTracking().SingleAsync(item => item.Id == customerId, Token);
        Assert.Equal(phone, customer.Phone);
        Assert.Equal("คุณผู้มาใหม่ จองเอง", customer.DisplayName);
        Assert.NotNull(customer.UserId);

        var spent = await db.Set<SlotHold>().AsNoTracking().SingleAsync(item => item.Id == hold.GetProperty("id").GetString(), Token);
        Assert.NotNull(spent.ConsumedAt);
    }

    [Fact]
    public async Task Signing_up_here_records_the_notice_and_the_consents_that_were_given()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var withMarketing = NewPhone();
        var without = NewPhone();
        using var client = fixture.CreateClient();

        var first = await VerifyAsync(client, (await StartAsync(client, clinic, withMarketing, 5)).GetProperty("id").GetString()!, withMarketing, marketing: true);
        var second = await VerifyAsync(client, (await StartAsync(client, clinic, without, 6)).GetProperty("id").GetString()!, without);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        async Task<List<CustomerConsent>> RowsFor(JsonElement verified)
        {
            var customerId = verified.GetProperty("capabilities").GetProperty("customer_id").GetString();
            return await db.Set<CustomerConsent>().AsNoTracking().Where(row => row.CustomerId == customerId).ToListAsync(Token);
        }

        var granted = await RowsFor(first);
        // policy_version "2026-10" was sent and is ignored: the server stamps the notice current now
        // (docs/contract-extensions.md § 35), and nothing is published in this collection.
        Assert.Contains(granted, row => row.Purpose == ConsentPurpose.ServiceMessages && row.Granted && row.PolicyVersion == null && row.Source == ConsentSource.Portal);
        Assert.Contains(granted, row => row.Purpose == ConsentPurpose.Marketing && row.Granted && row.PolicyVersion == null);

        var serviceOnly = await RowsFor(second);
        Assert.Single(serviceOnly);
        Assert.Equal(ConsentPurpose.ServiceMessages, serviceOnly[0].Purpose);
    }

    [Fact]
    public async Task Without_accepting_the_privacy_notice_nothing_is_verified()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        var hold = await StartAsync(client, clinic, phone, 5);

        var response = await client.PostAsJsonAsync(
            "/booking/guest/verify",
            new { hold_id = hold.GetProperty("id").GetString(), phone, code = CodeFor(phone), display_name = "x", accept_privacy_notice = false },
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("accept_privacy_notice", (await ErrorOf(response)).GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_number_the_counter_knows_books_under_that_customer_and_sees_its_pets()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = await CustomerPhoneAsync(clinic.CustomerId);
        using var client = fixture.CreateClient();

        var hold = await StartAsync(client, clinic, phone, 5);
        var verified = await VerifyAsync(client, hold.GetProperty("id").GetString()!, phone);

        Assert.Equal(clinic.CustomerId, verified.GetProperty("capabilities").GetProperty("customer_id").GetString());
        Assert.Equal(clinic.PetId, verified.GetProperty("pets")[0].GetProperty("id").GetString());

        SignIn(client, verified);
        using var book = Idempotent("/portal/appointments", BookBody(hold, clinic.PetId));
        var booked = await client.SendAsync(book, Token);

        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var appointment = await booked.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(clinic.CustomerId, appointment.GetProperty("customer_id").GetString());
        Assert.Equal(clinic.PetId, appointment.GetProperty("pet_id").GetString());
    }

    [Fact]
    public async Task A_number_with_a_login_signs_in_to_it_and_keeps_its_consents()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();

        await client.PostAsJsonAsync("/auth/otp/request", new { phone, purpose = "register" }, Token);
        var registered = await client.PostAsJsonAsync("/auth/register", new { phone, code = CodeFor(phone), display_name = "คุณเคยสมัคร" }, Token);
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var customerId = (await registered.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("capabilities").GetProperty("customer_id").GetString();

        var hold = await StartAsync(client, clinic, phone, 5);
        var verified = await VerifyAsync(client, hold.GetProperty("id").GetString()!, phone, marketing: true);

        Assert.False(verified.GetProperty("account_created").GetBoolean());
        Assert.Equal(customerId, verified.GetProperty("capabilities").GetProperty("customer_id").GetString());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.False(await db.Set<CustomerConsent>().AnyAsync(row => row.CustomerId == customerId, Token));
        Assert.Equal(1, await db.Set<UserAccount>().CountAsync(account => account.PhoneNumber == phone, Token));
    }

    [Fact]
    public async Task Before_the_code_an_existing_number_and_a_new_one_get_the_same_answers()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var known = await CustomerPhoneAsync(clinic.CustomerId);
        var stranger = NewPhone();
        using var client = fixture.CreateClient();

        var forKnown = await client.PostAsJsonAsync("/booking/guest/start", StartBody(clinic, known, 5), Token);
        var forStranger = await client.PostAsJsonAsync("/booking/guest/start", StartBody(clinic, stranger, 6), Token);

        Assert.Equal(forKnown.StatusCode, forStranger.StatusCode);

        var knownBody = await forKnown.Content.ReadFromJsonAsync<JsonElement>(Token);
        var strangerBody = await forStranger.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(Shape(knownBody), Shape(strangerBody));
        Assert.Equal(1, fixture.SignIn.CodesSentTo(known));
        Assert.Equal(1, fixture.SignIn.CodesSentTo(stranger));

        static string Shape(JsonElement body) => string.Join(",", body.EnumerateObject().Select(property =>
            property.Value.ValueKind == JsonValueKind.Object
                ? $"{property.Name}{{{string.Join(",", property.Value.EnumerateObject().Select(inner => inner.Name))}}}"
                : property.Name));

        // A wrong code says the same thing to both.
        var wrongKnown = await client.PostAsJsonAsync(
            "/booking/guest/verify", VerifyBody(knownBody.GetProperty("hold").GetProperty("id").GetString()!, known, Wrong(CodeFor(known))), Token);
        var wrongStranger = await client.PostAsJsonAsync(
            "/booking/guest/verify", VerifyBody(strangerBody.GetProperty("hold").GetProperty("id").GetString()!, stranger, Wrong(CodeFor(stranger))), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongKnown.StatusCode);
        Assert.Equal(wrongKnown.StatusCode, wrongStranger.StatusCode);

        var knownError = await ErrorOf(wrongKnown);
        var strangerError = await ErrorOf(wrongStranger);
        Assert.Equal(knownError.GetProperty("code").GetString(), strangerError.GetProperty("code").GetString());
        Assert.Equal(knownError.GetProperty("message").GetString(), strangerError.GetProperty("message").GetString());
        Assert.Equal("code", knownError.GetProperty("field").GetString());
        Assert.Equal("code", strangerError.GetProperty("field").GetString());
    }

    private static string Wrong(string code) => code == "000000" ? "111111" : "000000";

    [Fact]
    public async Task An_expired_code_is_refused_like_a_wrong_one()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        var hold = await StartAsync(client, clinic, phone, 5);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            await db.Set<PhoneVerification>()
                .Where(verification => verification.PhoneDigits == phone)
                .ExecuteUpdateAsync(set => set.SetProperty(verification => verification.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)), Token);
        }

        var response = await client.PostAsJsonAsync("/booking/guest/verify", VerifyBody(hold.GetProperty("id").GetString()!, phone, CodeFor(phone)), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("code", (await ErrorOf(response)).GetProperty("field").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task A_staff_number_gets_a_code_like_anyone_and_is_refused_after_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var staff = await IdentitySeed.StaffAsync(fixture, [Scopes.Appointment.Read], withTwoFactor: false, cancellationToken: Token);
        var phone = NewPhone();

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var account = await db.Set<UserAccount>().SingleAsync(item => item.Id == staff.UserId, Token);
            account.ChangePhone(phone);
            await db.SaveChangesAsync(Token);
        }

        using var client = fixture.CreateClient();
        var hold = await StartAsync(client, clinic, phone, 5);
        Assert.Equal(1, fixture.SignIn.CodesSentTo(phone));

        var response = await client.PostAsJsonAsync("/booking/guest/verify", VerifyBody(hold.GetProperty("id").GetString()!, phone, CodeFor(phone)), Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("contact_taken", (await ErrorOf(response)).GetProperty("code").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Empty(await IdentitySeed.SessionsAsync(fixture, staff.UserId, Token));

        using var check = fixture.CreateScope();
        var held = await check.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Set<SlotHold>().AsNoTracking()
            .SingleAsync(item => item.Id == hold.GetProperty("id").GetString(), Token);
        Assert.StartsWith(SlotHold.GuestHolderPrefix, held.HeldByCustomerId);
    }

    [Fact]
    public async Task A_fourth_code_for_one_number_is_refused_and_takes_no_time()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            await StartAsync(client, clinic, phone, 5 + i);
        }

        var fourth = await client.PostAsJsonAsync("/booking/guest/start", StartBody(clinic, phone, 9), Token);

        Assert.Equal(HttpStatusCode.TooManyRequests, fourth.StatusCode);
        Assert.Equal("rate_limited", (await ErrorOf(fourth)).GetProperty("code").GetString());

        // The refused attempt held nothing: 9 o'clock is still free for someone else.
        var other = await client.PostAsJsonAsync("/booking/guest/start", StartBody(clinic, NewPhone(), 9), Token);
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Fact]
    public async Task A_time_already_held_or_booked_is_refused_at_start()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = fixture.CreateClient();

        await StartAsync(client, clinic, NewPhone(), 5);
        await BookAsStaffAsync(fixture, clinic, 6, Token);

        var held = await client.PostAsJsonAsync("/booking/guest/start", StartBody(clinic, NewPhone(), 5), Token);
        var booked = await client.PostAsJsonAsync("/booking/guest/start", StartBody(clinic, NewPhone(), 6), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, held.StatusCode);
        Assert.Equal("slot_unavailable", (await ErrorOf(held)).GetProperty("code").GetString());
        Assert.Equal("slot_unavailable", (await ErrorOf(booked)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Starting_again_lets_go_of_the_time_held_before()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();

        await StartAsync(client, clinic, phone, 5);
        await StartAsync(client, clinic, phone, 5);
        await StartAsync(client, clinic, phone, 6);

        var someoneElse = await client.PostAsJsonAsync("/booking/guest/start", StartBody(clinic, NewPhone(), 5), Token);

        Assert.Equal(HttpStatusCode.Created, someoneElse.StatusCode);
    }

    [Fact]
    public async Task An_expired_hold_is_refused_before_the_code_is_spent()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        var hold = await StartAsync(client, clinic, phone, 5);
        var holdId = hold.GetProperty("id").GetString()!;

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            await db.Set<SlotHold>()
                .Where(item => item.Id == holdId)
                .ExecuteUpdateAsync(set => set.SetProperty(item => item.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)), Token);
        }

        var response = await client.PostAsJsonAsync("/booking/guest/verify", VerifyBody(holdId, phone, CodeFor(phone)), Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("hold_expired", (await ErrorOf(response)).GetProperty("code").GetString());

        using var check = fixture.CreateScope();
        var code = await check.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Set<PhoneVerification>().AsNoTracking()
            .SingleAsync(verification => verification.PhoneDigits == phone, Token);
        Assert.Null(code.ConsumedAt);
        Assert.Equal(0, code.Attempts);
    }

    [Fact]
    public async Task Another_visitors_hold_cannot_be_claimed()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var owner = NewPhone();
        var intruder = NewPhone();
        using var client = fixture.CreateClient();

        var theirs = await StartAsync(client, clinic, owner, 5);
        await StartAsync(client, clinic, intruder, 6);

        var response = await client.PostAsJsonAsync(
            "/booking/guest/verify", VerifyBody(theirs.GetProperty("id").GetString()!, intruder, CodeFor(intruder)), Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("hold_not_yours", (await ErrorOf(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_code_works_once()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        var hold = await StartAsync(client, clinic, phone, 5);
        var body = VerifyBody(hold.GetProperty("id").GetString()!, phone, CodeFor(phone));

        var first = await client.PostAsJsonAsync("/booking/guest/verify", body, Token);
        var again = await client.PostAsJsonAsync("/booking/guest/verify", body, Token);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task Confirming_twice_with_one_key_books_once_and_registers_one_pet()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        var hold = await StartAsync(client, clinic, phone, 5);
        var verified = await VerifyAsync(client, hold.GetProperty("id").GetString()!, phone);
        SignIn(client, verified);

        var key = Guid.NewGuid().ToString();
        var body = BookBody(hold, new { name = "มะม่วง", species = "สุนัข" });

        using var first = Idempotent("/portal/appointments", body, key);
        using var retry = Idempotent("/portal/appointments", body, key);
        var original = await client.SendAsync(first, Token);
        var replayed = await client.SendAsync(retry, Token);

        Assert.Equal(HttpStatusCode.Created, original.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replayed.StatusCode);
        Assert.Equal(
            (await original.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString(),
            (await replayed.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString());

        var customerId = verified.GetProperty("capabilities").GetProperty("customer_id").GetString();
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        Assert.Single(await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token));
        Assert.Equal(1, await db.Set<Pet>().CountAsync(pet => pet.PrimaryCustomerId == customerId, Token));
    }

    [Fact]
    public async Task A_refused_booking_leaves_no_new_pet_behind()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var phone = NewPhone();
        using var client = fixture.CreateClient();
        var hold = await StartAsync(client, clinic, phone, 5);
        var verified = await VerifyAsync(client, hold.GetProperty("id").GetString()!, phone);
        SignIn(client, verified);

        // Booking a different, already taken time with the new pet: refused, and nothing is kept.
        await BookAsStaffAsync(fixture, clinic, 7, Token);
        using var request = Idempotent("/portal/appointments", new
        {
            new_pet = new { name = "ส้มโอ", species = "แมว" },
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
            starts_at = ClinicSeed.SlotAt(7),
        });
        var response = await client.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var customerId = verified.GetProperty("capabilities").GetProperty("customer_id").GetString();
        using var scope = fixture.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Set<Pet>().AnyAsync(pet => pet.PrimaryCustomerId == customerId, Token));
    }

    [Fact]
    public async Task A_portal_booking_names_one_pet_either_way()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var customer = fixture.CustomerOf(clinic);

        using var neither = Idempotent("/portal/appointments", new { service_id = clinic.ServiceId, branch_id = clinic.BranchId, starts_at = ClinicSeed.SlotAt(5) });
        using var both = Idempotent("/portal/appointments", new
        {
            pet_id = clinic.PetId,
            new_pet = new { name = "ซ้ำ", species = "สุนัข" },
            service_id = clinic.ServiceId,
            branch_id = clinic.BranchId,
            starts_at = ClinicSeed.SlotAt(5),
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await customer.SendAsync(neither, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await customer.SendAsync(both, Token)).StatusCode);
    }

    [Fact]
    public async Task Booking_codes_are_not_handed_out_by_the_sign_in_endpoint()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/otp/request", new { phone = NewPhone(), purpose = "booking" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
