using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Clinics;

/// <summary>
/// What the clinic tells the public about itself, and the writes behind it —
/// "อ่านได้แบบ public · แก้ได้เฉพาะเจ้าของและแอดมิน".
/// <para>
/// On the separate instance: the profile and the opening hours are one row each for the whole
/// clinic, so changing them in the shared database would change them for every other test.
/// </para>
/// </summary>
[Collection(nameof(FreshInstanceCollection))]
public class ClinicProfileAndBranchTests(ApiFixture fixture) : FreshInstanceTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    protected override ApiFixture Instance => fixture;

    private HttpClient Admin(params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: scopes.Length == 0 ? [Scopes.Settings.Read, Scopes.Settings.Write] : scopes));

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private static object Week(string opens = "09:00", string closes = "18:00") => new
    {
        days = new object[]
        {
            new { day = "sunday", closed = true },
            new { day = "monday", opens, closes },
            new { day = "tuesday", opens, closes },
            new { day = "wednesday", opens, closes },
            new { day = "thursday", opens, closes },
            new { day = "friday", opens, closes },
            new { day = "saturday", opens = "10:00", closes = "16:00" },
        },
    };

    [Fact]
    public async Task The_profile_is_public_and_editable_only_with_settings_write()
    {
        using var admin = Admin();
        using var anonymous = fixture.CreateClient();

        var updated = await admin.PatchAsJsonAsync(
            "/clinic",
            new
            {
                name = "แฮปปี้พอว์ส สุขุมวิท",
                brand_color = "#1f7a6d",
                license_no = "สพ. 12/2567",
                operator_name = "น.สพ. สมชาย ใจดี",
                logo_url = "https://cdn.happypaws.test/logo.png",
            },
            Token);

        updated.EnsureSuccessStatusCode();

        // "ชื่อ โลโก้ สี ใบอนุญาต ผู้ควบคุมกิจการ" — all of it readable with no token at all.
        var profile = await anonymous.GetFromJsonAsync<JsonElement>("/clinic", Token);

        Assert.Equal("แฮปปี้พอว์ส สุขุมวิท", profile.GetProperty("name").GetString());
        Assert.Equal("#1F7A6D", profile.GetProperty("brand_color").GetString());
        Assert.Equal("สพ. 12/2567", profile.GetProperty("license_no").GetString());
        Assert.Equal("น.สพ. สมชาย ใจดี", profile.GetProperty("operator_name").GetString());

        var refused = await anonymous.PatchAsJsonAsync("/clinic", new { name = "x" }, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);

        using var reader = Admin(Scopes.Settings.Read);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await reader.PatchAsJsonAsync("/clinic", new { name = "x" }, Token)).StatusCode);
    }

    [Fact]
    public async Task A_field_that_was_not_sent_is_left_alone_and_an_empty_one_is_cleared()
    {
        using var admin = Admin();

        (await admin.PatchAsJsonAsync(
            "/clinic", new { license_no = "สพ. 1/2560", operator_name = "น.สพ. ทดสอบ" }, Token)).EnsureSuccessStatusCode();

        var patched = await BodyAsync(await admin.PatchAsJsonAsync("/clinic", new { license_no = "" }, Token));

        Assert.False(patched.TryGetProperty("license_no", out _));
        Assert.Equal("น.สพ. ทดสอบ", patched.GetProperty("operator_name").GetString());
    }

    [Theory]
    [InlineData("logo_url", "javascript:alert(1)")]
    [InlineData("logo_url", "/relative.png")]
    [InlineData("brand_color", "teal")]
    [InlineData("name", "")]
    public async Task A_malformed_profile_value_is_a_validation_error(string field, string value)
    {
        using var admin = Admin();

        var response = await admin.PatchAsJsonAsync(
            "/clinic", new Dictionary<string, string> { [field] = value }, Token);

        // The logo address is rendered into an img on a public page, so a javascript: address
        // here would be a stored XSS on the clinic's own website.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(field, (await BodyAsync(response)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task A_new_branch_takes_its_id_from_its_code_and_appears_on_the_public_list()
    {
        using var admin = Admin();
        using var anonymous = fixture.CreateClient();

        var code = $"b{Guid.NewGuid():N}"[..12];

        var created = await admin.PostAsJsonAsync(
            "/clinic/branches",
            new
            {
                code,
                name = "สาขาทองหล่อ",
                address = "55 ซอยทองหล่อ กรุงเทพฯ",
                latitude = 13.7325,
                longitude = 100.5786,
                exam_rooms = new[] { "room_1", "room_2" },
            },
            Token);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        // As the contract shows it: brn_sukhumvit39.
        Assert.Equal($"brn_{code}", (await BodyAsync(created)).GetProperty("id").GetString());

        var branches = await anonymous.GetFromJsonAsync<JsonElement>("/clinic/branches", Token);
        var listed = branches.GetProperty("data").EnumerateArray()
            .Single(branch => branch.GetProperty("code").GetString() == code);

        // "สาขา ที่อยู่ พิกัด ห้องตรวจ".
        Assert.Equal("55 ซอยทองหล่อ กรุงเทพฯ", listed.GetProperty("address").GetString());
        Assert.Equal(13.7325, listed.GetProperty("latitude").GetDouble());
        Assert.Equal(["room_1", "room_2"], listed.GetProperty("exam_rooms").EnumerateArray().Select(r => r.GetString()));

        var again = await admin.PostAsJsonAsync("/clinic/branches", new { code, name = "ซ้ำ" }, Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("duplicate_branch", (await BodyAsync(again)).GetProperty("error").GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("Has Spaces")]
    [InlineData("x")]
    public async Task A_branch_code_that_cannot_be_an_id_is_refused(string code)
    {
        using var admin = Admin();

        var response = await admin.PostAsJsonAsync("/clinic/branches", new { code, name = "สาขา" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Half_a_coordinate_is_refused()
    {
        using var admin = Admin();

        var response = await admin.PostAsJsonAsync(
            "/clinic/branches", new { code = $"b{Guid.NewGuid():N}"[..12], name = "สาขา", latitude = 13.7 }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task The_whole_week_is_set_in_one_request_and_read_back_monday_first()
    {
        using var admin = Admin();
        using var anonymous = fixture.CreateClient();

        (await admin.PutAsJsonAsync("/clinic/hours", Week(), Token)).EnsureSuccessStatusCode();

        var hours = await anonymous.GetFromJsonAsync<JsonElement>("/clinic/hours", Token);
        var days = hours.GetProperty("days").EnumerateArray().ToList();

        Assert.True(hours.GetProperty("configured").GetBoolean());
        Assert.Equal(7, days.Count);
        Assert.Equal("monday", days[0].GetProperty("day").GetString());
        Assert.Equal("09:00", days[0].GetProperty("opens").GetString());
        Assert.Equal("18:00", days[0].GetProperty("closes").GetString());

        // "วันหยุดประจำสัปดาห์".
        var sunday = days[^1];

        Assert.Equal("sunday", sunday.GetProperty("day").GetString());
        Assert.True(sunday.GetProperty("closed").GetBoolean());
        Assert.False(sunday.TryGetProperty("opens", out _));
    }

    [Fact]
    public async Task A_week_missing_a_day_is_refused_rather_than_merged()
    {
        using var admin = Admin();

        var response = await admin.PutAsJsonAsync(
            "/clinic/hours",
            new { days = new object[] { new { day = "monday", opens = "09:00", closes = "18:00" } } },
            Token);

        // "ตั้งทั้งสัปดาห์ในคำขอเดียว" — a whole-set replacement, so a partial one is an error.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("18:00", "09:00")]
    [InlineData("9am", "18:00")]
    public async Task A_day_with_impossible_hours_is_refused(string opens, string closes)
    {
        using var admin = Admin();

        var response = await admin.PutAsJsonAsync("/clinic/hours", Week(opens, closes), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_holiday_is_listed_with_the_hours_and_cannot_be_declared_twice()
    {
        using var admin = Admin();
        using var anonymous = fixture.CreateClient();

        // Far enough ahead that it can never fall on a day another test books.
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(300 + Random.Shared.Next(60))).ToString("yyyy-MM-dd");

        var declared = await admin.PostAsJsonAsync("/clinic/holidays", new { date, name = "ปิดปรับปรุง" }, Token);

        Assert.Equal(HttpStatusCode.Created, declared.StatusCode);

        var hours = await anonymous.GetFromJsonAsync<JsonElement>("/clinic/hours", Token);

        // The contract declares holidays and gives no endpoint that reads them back.
        Assert.Contains(
            hours.GetProperty("holidays").EnumerateArray(),
            holiday => holiday.GetProperty("date").GetString() == date
                && holiday.GetProperty("name").GetString() == "ปิดปรับปรุง");

        var again = await admin.PostAsJsonAsync("/clinic/holidays", new { date, name = "อีกครั้ง" }, Token);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task A_holiday_in_the_past_or_at_an_unknown_branch_is_refused()
    {
        using var admin = Admin();

        var past = await admin.PostAsJsonAsync(
            "/clinic/holidays", new { date = "2020-01-01", name = "ปีใหม่" }, Token);

        var nowhere = await admin.PostAsJsonAsync(
            "/clinic/holidays",
            new { date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(400)).ToString("yyyy-MM-dd"), name = "ปิด", branch_id = "brn_nowhere" },
            Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, past.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, nowhere.StatusCode);
    }

    [Fact]
    public async Task Declaring_a_holiday_closes_booking_for_that_day_at_that_branch()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var open = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var admin = Admin();
        using var anonymous = fixture.CreateClient();

        var startsAt = ClinicSeed.SlotAt(4);
        var day = ClinicSeed.ClinicDate(startsAt);

        async Task<JsonElement> FeedAsync(SeededClinic at) =>
            await anonymous.GetFromJsonAsync<JsonElement>(
                $"/booking/availability?service={at.ServiceId}&date={day}&branch={at.BranchId}", Token);

        Assert.NotEmpty((await FeedAsync(clinic)).GetProperty("slots").EnumerateArray());

        (await admin.PostAsJsonAsync(
            "/clinic/holidays", new { date = day, name = "ปิดเฉพาะสาขา", branch_id = clinic.BranchId }, Token))
            .EnsureSuccessStatusCode();

        // "ปิดรับนัดอัตโนมัติ": nothing else had to be told. The feed is empty —
        Assert.Empty((await FeedAsync(clinic)).GetProperty("slots").EnumerateArray());

        // — a booking for that day is refused, for the counter as much as for the customer —
        using var staff = fixture.StaffOf(clinic);
        using var booking = Idempotent("/staff/appointments", Booking(clinic, startsAt));

        var refused = await staff.SendAsync(booking, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Equal("slot_unavailable", (await BodyAsync(refused)).GetProperty("error").GetProperty("code").GetString());

        // — and the branch that did not close is untouched.
        Assert.NotEmpty((await FeedAsync(open)).GetProperty("slots").EnumerateArray());
    }
}
