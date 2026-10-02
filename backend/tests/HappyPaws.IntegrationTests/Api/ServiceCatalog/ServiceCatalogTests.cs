using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.ServiceCatalog;

/// <summary>
/// The service catalogue: what the public menu shows, what a price is on a given day, and where
/// and when a service can be booked.
/// </summary>
[Collection(nameof(ApiCollection))]
public class ServiceCatalogTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TimeZoneInfo ClinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    private HttpClient CatalogueManager(SeededClinic clinic) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Service.Manage], [clinic.BranchId]));

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private static async Task<JsonElement> CreateAsync(HttpClient manager, object? overrides = null)
    {
        var response = await manager.PostAsJsonAsync(
            "/staff/services",
            overrides ?? new
            {
                name = $"ขูดหินปูน {Guid.NewGuid():N}"[..20],
                category_code = "dental",
                duration_minutes = 45,
                price_satang = 120_000,
            },
            Token);

        response.EnsureSuccessStatusCode();

        return await BodyAsync(response);
    }

    private async Task<List<string?>> PublicMenuIdsAsync(string query = "")
    {
        // No Authorization header: the contract marks the menu public.
        using var anonymous = fixture.CreateClient();

        var menu = await anonymous.GetFromJsonAsync<JsonElement>($"/services{query}", Token);

        return [.. menu.GetProperty("data").EnumerateArray().Select(service => service.GetProperty("id").GetString())];
    }

    private static string ClinicToday() =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ClinicTimeZone).ToString("yyyy-MM-dd");

    private static string ClinicDay(int daysFromToday) =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ClinicTimeZone).AddDays(daysFromToday).ToString("yyyy-MM-dd");

    [Fact]
    public async Task The_categories_are_the_contracts_six_in_the_contracts_order()
    {
        using var anonymous = fixture.CreateClient();

        var categories = await anonymous.GetFromJsonAsync<JsonElement>("/services/categories", Token);

        // "ตรวจ วัคซีน ทันตกรรม ผ่าตัด แล็บ กรูมมิ่ง".
        Assert.Equal(
            ["checkup", "vaccine", "dental", "surgery", "lab", "grooming"],
            categories.GetProperty("data").EnumerateArray().Select(category => category.GetProperty("code").GetString()));
    }

    [Fact]
    public async Task A_new_service_is_not_on_the_public_menu_until_someone_opens_it()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var service = await CreateAsync(manager);
        var id = service.GetProperty("id").GetString()!;

        Assert.False(service.GetProperty("online_booking_enabled").GetBoolean());
        Assert.DoesNotContain(id, await PublicMenuIdsAsync());

        (await manager.PatchAsJsonAsync(
            $"/staff/services/{id}/availability", new { online_booking_enabled = true }, Token)).EnsureSuccessStatusCode();

        // "เฉพาะบริการที่เปิดให้จองออนไลน์".
        Assert.Contains(id, await PublicMenuIdsAsync());
        Assert.Contains(id, await PublicMenuIdsAsync("?category=dental"));
        Assert.DoesNotContain(id, await PublicMenuIdsAsync("?category=grooming"));
    }

    [Fact]
    public async Task The_public_menu_does_not_carry_what_only_staff_should_see()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var anonymous = fixture.CreateClient();

        var menu = await anonymous.GetFromJsonAsync<JsonElement>($"/services?branch={clinic.BranchId}", Token);
        var seeded = menu.GetProperty("data").EnumerateArray()
            .Single(service => service.GetProperty("id").GetString() == clinic.ServiceId);

        Assert.Equal(50_000, seeded.GetProperty("price_satang").GetInt64());
        Assert.False(seeded.TryGetProperty("eligible_staff_ids", out _));
        Assert.False(seeded.TryGetProperty("upcoming_pricing", out _));
        Assert.False(seeded.TryGetProperty("version", out _));
    }

    [Fact]
    public async Task A_code_becomes_the_readable_id_and_cannot_be_used_twice()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var code = $"xray-{Guid.NewGuid():N}"[..16];
        var body = new { name = "เอกซเรย์", category_code = "lab", duration_minutes = 20, price_satang = 80_000, code };

        var service = await CreateAsync(manager, body);

        // As the contract's own example has it: svc_checkup.
        Assert.Equal($"svc_{code}", service.GetProperty("id").GetString());

        var again = await manager.PostAsJsonAsync("/staff/services", body, Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("duplicate_service", (await BodyAsync(again)).GetProperty("error").GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("boarding", 30, 1000)]
    [InlineData("dental", 2, 1000)]
    [InlineData("dental", 30, -1)]
    public async Task A_service_with_an_unknown_category_or_an_impossible_shape_is_refused(
        string category,
        int minutes,
        long price)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var response = await manager.PostAsJsonAsync(
            "/staff/services",
            new { name = "บริการ", category_code = category, duration_minutes = minutes, price_satang = price },
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_provider_who_is_not_on_the_staff_cannot_be_named_on_a_service()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var response = await manager.PostAsJsonAsync(
            "/staff/services",
            new
            {
                name = "ผ่าตัดทำหมัน",
                category_code = "surgery",
                duration_minutes = 60,
                price_satang = 350_000,
                eligible_staff_ids = new[] { clinic.StaffId, "stf_nobody" },
            },
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            "eligible_staff_ids",
            (await BodyAsync(response)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_price_dated_tomorrow_is_held_back_until_tomorrow()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var id = (await CreateAsync(manager)).GetProperty("id").GetString();

        var response = await manager.PatchAsJsonAsync(
            $"/staff/services/{id}/pricing",
            new { base_price_satang = 150_000, effective_from = ClinicDay(1) },
            Token);

        response.EnsureSuccessStatusCode();

        var service = await BodyAsync(response);

        // "ราคามีผลวันที่": today's customer is still quoted today's price.
        Assert.Equal(120_000, service.GetProperty("price_satang").GetInt64());

        var upcoming = service.GetProperty("upcoming_pricing").EnumerateArray().Single();

        Assert.Equal(150_000, upcoming.GetProperty("base_price_satang").GetInt64());
        Assert.Equal(ClinicDay(1), upcoming.GetProperty("effective_from").GetString());
    }

    [Fact]
    public async Task A_price_dated_today_applies_at_once_with_its_tiers()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var id = (await CreateAsync(manager)).GetProperty("id").GetString();

        var response = await manager.PatchAsJsonAsync(
            $"/staff/services/{id}/pricing",
            new
            {
                base_price_satang = 100_000,
                tiers = new object[]
                {
                    new { species = "แมว", price_satang = 80_000 },
                    new { min_weight_kg = 20, price_satang = 160_000 },
                },
            },
            Token);

        response.EnsureSuccessStatusCode();

        var service = await BodyAsync(response);

        Assert.Equal(100_000, service.GetProperty("price_satang").GetInt64());

        // "ราคาตามน้ำหนัก/ชนิดสัตว์".
        var tiers = service.GetProperty("pricing").GetProperty("tiers").EnumerateArray().ToList();

        Assert.Equal(2, tiers.Count);
        Assert.Equal("แมว", tiers[0].GetProperty("species").GetString());
        Assert.Equal(20, tiers[1].GetProperty("min_weight_kg").GetDecimal());
        Assert.Equal(ClinicToday(), service.GetProperty("pricing").GetProperty("effective_from").GetString());
    }

    [Fact]
    public async Task A_price_cannot_be_backdated()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var id = (await CreateAsync(manager)).GetProperty("id").GetString();

        var response = await manager.PatchAsJsonAsync(
            $"/staff/services/{id}/pricing",
            new { base_price_satang = 10_000, effective_from = ClinicDay(-1) },
            Token);

        // A price list that started last week would say visits already billed cost something else.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("effective_from", (await BodyAsync(response)).GetProperty("error").GetProperty("field").GetString());
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, 10, 5)]
    public async Task A_tier_that_describes_nothing_or_an_impossible_band_is_refused(
        string? species,
        int? minWeight,
        int? maxWeight)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var id = (await CreateAsync(manager)).GetProperty("id").GetString();

        var response = await manager.PatchAsJsonAsync(
            $"/staff/services/{id}/pricing",
            new
            {
                base_price_satang = 100_000,
                tiers = new[] { new { species, min_weight_kg = minWeight, max_weight_kg = maxWeight, price_satang = 1 } },
            },
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_service_limited_to_other_hours_is_off_the_public_feed_but_still_bookable_at_the_counter()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var startsAt = ClinicSeed.SlotAt(4);
        var local = TimeZoneInfo.ConvertTime(startsAt, ClinicTimeZone);

        // A window on a different weekday from the slot being asked about, so whatever time the
        // suite runs the slot falls outside it.
        var otherDay = local.AddDays(3).DayOfWeek.ToString().ToLowerInvariant();

        (await manager.PatchAsJsonAsync(
            $"/staff/services/{clinic.ServiceId}/availability",
            new { booking_windows = new[] { new { day = otherDay, from = "09:00", to = "12:00" } } },
            Token)).EnsureSuccessStatusCode();

        using var anonymous = fixture.CreateClient();

        var feed = await anonymous.GetFromJsonAsync<JsonElement>(
            $"/booking/availability?service={clinic.ServiceId}&date={ClinicSeed.ClinicDate(startsAt)}&branch={clinic.BranchId}",
            Token);

        // "จำกัด…ช่วงเวลา": nothing is offered online outside the window —
        Assert.Empty(feed.GetProperty("slots").EnumerateArray());

        using var customer = fixture.CustomerOf(clinic);
        using var selfService = Idempotent("/portal/appointments", Booking(clinic, startsAt));

        var refused = await customer.SendAsync(selfService, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Equal("slot_unavailable", (await BodyAsync(refused)).GetProperty("error").GetProperty("code").GetString());

        // — and the counter, who takes these by phone, is not bound by it.
        using var staff = fixture.StaffOf(clinic);
        using var byPhone = Idempotent("/staff/appointments", Booking(clinic, startsAt));

        Assert.Equal(HttpStatusCode.Created, (await staff.SendAsync(byPhone, Token)).StatusCode);
    }

    [Fact]
    public async Task A_service_limited_to_another_branch_cannot_be_booked_here_by_anyone()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        (await manager.PatchAsJsonAsync(
            $"/staff/services/{clinic.ServiceId}/availability",
            new { branch_ids = new[] { elsewhere.BranchId } },
            Token)).EnsureSuccessStatusCode();

        using var anonymous = fixture.CreateClient();

        var feed = await anonymous.GetAsync(
            $"/booking/availability?service={clinic.ServiceId}&date={ClinicSeed.ClinicDate(ClinicSeed.SlotAt(4))}&branch={clinic.BranchId}",
            Token);

        Assert.Equal(HttpStatusCode.NotFound, feed.StatusCode);

        // "จำกัดสาขา" binds the counter too: the service does not exist at this site.
        using var staff = fixture.StaffOf(clinic);
        using var booking = Idempotent("/staff/appointments", Booking(clinic, ClinicSeed.SlotAt(4)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await staff.SendAsync(booking, Token)).StatusCode);

        Assert.DoesNotContain(clinic.ServiceId, await PublicMenuIdsAsync($"?branch={clinic.BranchId}"));
        Assert.Contains(clinic.ServiceId, await PublicMenuIdsAsync($"?branch={elsewhere.BranchId}"));
    }

    [Fact]
    public async Task A_branch_that_does_not_exist_cannot_be_named_on_a_service()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = CatalogueManager(clinic);

        var response = await manager.PatchAsJsonAsync(
            $"/staff/services/{clinic.ServiceId}/availability",
            new { branch_ids = new[] { "brn_nowhere" } },
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/staff/services")]
    [InlineData("PATCH", "/staff/services/svc_x/pricing")]
    [InlineData("PATCH", "/staff/services/svc_x/availability")]
    public async Task Every_service_write_requires_service_manage(string method, string path)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var receptionist = fixture.StaffOf(clinic);
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = JsonContent.Create(new { }),
        };

        var response = await receptionist.SendAsync(request, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
