using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.ServiceCatalog;

/// <summary>
/// The staff list and edit of services — a contract extension, <c>docs/contract-extensions.md § 17</c>.
/// <para>
/// Other tests share the database, so each test finds its own services by id rather than asserting
/// on the whole list.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class ServiceManagementTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Manager(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Service.Manage] : scopes,
            [clinic.BranchId]));

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private static async Task<JsonElement> CreateAsync(HttpClient manager, bool online = false)
    {
        var response = await manager.PostAsJsonAsync(
            "/staff/services",
            new
            {
                name = $"ขูดหินปูน {Guid.NewGuid():N}"[..20],
                category_code = "dental",
                duration_minutes = 45,
                price_satang = 120_000,
                online_booking_enabled = online,
            },
            Token);

        response.EnsureSuccessStatusCode();

        return await BodyAsync(response);
    }

    private static async Task<JsonElement?> FindAsync(HttpClient client, string path, string id)
    {
        var list = await client.GetFromJsonAsync<JsonElement>(path, Token);

        foreach (var service in list.GetProperty("data").EnumerateArray())
        {
            if (service.GetProperty("id").GetString() == id)
            {
                return service;
            }
        }

        return null;
    }

    [Fact]
    public async Task The_staff_list_has_counter_only_services_the_public_menu_does_not()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = Manager(clinic);
        using var anyone = fixture.CreateClient();

        var id = (await CreateAsync(manager)).GetProperty("id").GetString()!;

        var listed = await FindAsync(manager, "/staff/services", id);

        Assert.NotNull(listed);
        Assert.False(listed.Value.GetProperty("online_booking_enabled").GetBoolean());
        Assert.True(listed.Value.TryGetProperty("version", out _));
        Assert.Null(await FindAsync(anyone, "/services", id));

        Assert.NotNull(await FindAsync(manager, "/staff/services?category=dental", id));
        Assert.Null(await FindAsync(manager, "/staff/services?category=grooming", id));
    }

    [Fact]
    public async Task A_service_is_renamed_moved_and_lengthened_in_one_call()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = Manager(clinic);

        var id = (await CreateAsync(manager)).GetProperty("id").GetString()!;

        var response = await manager.PatchAsJsonAsync(
            $"/staff/services/{id}",
            new { name = "ขัดฟันและขูดหินปูน", category_code = "checkup", duration_minutes = 60, eligible_staff_ids = new[] { clinic.StaffId } },
            Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);

        var service = await BodyAsync(response);

        Assert.Equal("ขัดฟันและขูดหินปูน", service.GetProperty("name").GetString());
        Assert.Equal("checkup", service.GetProperty("category_code").GetString());
        Assert.Equal(60, service.GetProperty("duration_minutes").GetInt32());
        Assert.Equal([clinic.StaffId], service.GetProperty("eligible_staff_ids").EnumerateArray().Select(staff => staff.GetString()));

        // The price is the pricing endpoint's to change, and an edit here leaves it alone.
        Assert.Equal(120_000, service.GetProperty("price_satang").GetInt64());
    }

    [Fact]
    public async Task Taking_a_service_off_sale_removes_it_from_the_public_menu_until_it_is_back()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = Manager(clinic);
        using var anyone = fixture.CreateClient();

        var id = (await CreateAsync(manager, online: true)).GetProperty("id").GetString()!;

        Assert.NotNull(await FindAsync(anyone, "/services", id));

        var off = await BodyAsync(await manager.PatchAsJsonAsync($"/staff/services/{id}", new { is_active = false }, Token));

        Assert.False(off.GetProperty("is_active").GetBoolean());
        Assert.Null(await FindAsync(anyone, "/services", id));

        // Still on the staff list, so it can be found and put back.
        Assert.NotNull(await FindAsync(manager, "/staff/services", id));

        (await manager.PatchAsJsonAsync($"/staff/services/{id}", new { is_active = true }, Token)).EnsureSuccessStatusCode();

        Assert.NotNull(await FindAsync(anyone, "/services", id));
    }

    [Theory]
    [InlineData("{\"name\":\"  \"}", "name")]
    [InlineData("{\"category_code\":\"spa\"}", "category_code")]
    [InlineData("{\"duration_minutes\":4}", "duration_minutes")]
    [InlineData("{\"eligible_staff_ids\":[\"stf_nobody\"]}", "eligible_staff_ids")]
    public async Task A_bad_value_is_refused_on_its_field(string json, string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = Manager(clinic);

        var id = (await CreateAsync(manager)).GetProperty("id").GetString()!;

        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await manager.PatchAsync($"/staff/services/{id}", content, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(field, (await BodyAsync(response)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task Both_routes_need_service_manage_and_a_stale_version_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var manager = Manager(clinic);
        using var reader = Manager(clinic, Scopes.Settings.Read);

        var created = await CreateAsync(manager);
        var id = created.GetProperty("id").GetString()!;
        var version = created.GetProperty("version").GetUInt32();

        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync("/staff/services", Token)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await reader.PatchAsJsonAsync($"/staff/services/{id}", new { name = "x" }, Token)).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await manager.PatchAsJsonAsync("/staff/services/svc_nothing_here", new { name = "x" }, Token)).StatusCode);

        (await manager.PatchAsJsonAsync($"/staff/services/{id}", new { duration_minutes = 30 }, Token)).EnsureSuccessStatusCode();

        using var stale = new HttpRequestMessage(HttpMethod.Patch, $"/staff/services/{id}")
        {
            Content = JsonContent.Create(new { duration_minutes = 90 }),
        };
        stale.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");

        Assert.Equal(HttpStatusCode.Conflict, (await manager.SendAsync(stale, Token)).StatusCode);
    }
}
