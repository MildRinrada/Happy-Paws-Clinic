using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HappyPaws.IntegrationTests.Api.Clinics;

/// <summary>
/// <c>docs/contract-extensions.md § 50</c>: the clinic's own logo and an optional tab icon are
/// uploaded through signed URLs, named in the profile by <c>storage:</c> values, and read by anyone
/// through the fixed public addresses <c>GET /clinic/logo</c> and <c>GET /clinic/tab-icon</c>.
/// <para>
/// On the separate instance, because the profile is one row for the whole clinic.
/// </para>
/// </summary>
[Collection(nameof(FreshInstanceCollection))]
public class ClinicLogoTests(ApiFixture fixture) : FreshInstanceTest
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 5, 0, 5, 0];

    private const string Hex = "0123456789abcdef0123456789abcdef";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    protected override ApiFixture Instance => fixture;

    private HttpClient Admin(params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: scopes.Length == 0 ? [Scopes.Settings.Read, Scopes.Settings.Write] : scopes));

    /// <summary>Anyone at all, reporting a redirect instead of following it.</summary>
    private HttpClient Anonymous() => fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static async Task<JsonElement> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    /// <summary>Asks for an upload URL and sends the file to it; returns the profile value that names it.</summary>
    private async Task<string> UploadAsync(HttpClient admin, string route)
    {
        var started = await OkAsync(await admin.PostAsJsonAsync(route, new { content_type = "image/png", size_bytes = Png.Length }, Token));

        var upload = started.GetProperty("upload");
        Assert.Equal("PUT", upload.GetProperty("method").GetString());

        using var anyone = fixture.CreateClient();
        using var content = new ByteArrayContent(Png);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var put = await anyone.PutAsync(new Uri(upload.GetProperty("url").GetString()!).PathAndQuery, content, Token);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        return started.GetProperty("value").GetString()!;
    }

    /// <summary>Follows the public address as a browser would, and checks the picture behind it.</summary>
    private async Task AssertServedAsync(string route)
    {
        using var anonymous = Anonymous();

        var redirect = await anonymous.GetAsync($"{route}?v=any", Token);
        Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);
        Assert.True(redirect.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromMinutes(5), redirect.Headers.CacheControl?.MaxAge);

        var image = await anonymous.GetAsync(redirect.Headers.Location!.PathAndQuery, Token);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Contains("nosniff", image.Headers.GetValues("X-Content-Type-Options"));
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync(Token));
    }

    [Theory]
    [InlineData("/clinic/logo")]
    [InlineData("/clinic/tab-icon")]
    public async Task Asking_for_an_upload_needs_settings_write(string route)
    {
        using var anonymous = Anonymous();
        var unsigned = await anonymous.PostAsJsonAsync(route, new { content_type = "image/png", size_bytes = 10 }, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, unsigned.StatusCode);

        using var reader = Admin(Scopes.Settings.Read);
        var refused = await reader.PostAsJsonAsync(route, new { content_type = "image/png", size_bytes = 10 }, Token);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
    }

    [Theory]
    [InlineData("/clinic/logo", "image/svg+xml", 100)]
    [InlineData("/clinic/logo", "image/gif", 100)]
    [InlineData("/clinic/logo", "text/html", 100)]
    [InlineData("/clinic/logo", "image/png", 0)]
    [InlineData("/clinic/logo", "image/png", 6 * 1024 * 1024)]
    [InlineData("/clinic/tab-icon", "image/svg+xml", 100)]
    [InlineData("/clinic/tab-icon", "image/png", 6 * 1024 * 1024)]
    public async Task Only_a_png_jpeg_or_webp_of_at_most_5_mb_is_accepted(string route, string contentType, long sizeBytes)
    {
        using var admin = Admin();

        var response = await admin.PostAsJsonAsync(route, new { content_type = contentType, size_bytes = sizeBytes }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Asking_for_an_upload_does_not_change_the_profile()
    {
        using var admin = Admin();
        using var anonymous = Anonymous();
        var before = await anonymous.GetFromJsonAsync<JsonElement>("/clinic", Token);

        await OkAsync(await admin.PostAsJsonAsync("/clinic/logo", new { content_type = "image/webp", size_bytes = 900 }, Token));

        var after = await anonymous.GetFromJsonAsync<JsonElement>("/clinic", Token);
        Assert.Equal(before.GetProperty("version").GetUInt32(), after.GetProperty("version").GetUInt32());
    }

    [Fact]
    public async Task A_saved_logo_and_tab_icon_are_readable_by_anyone_through_fixed_addresses()
    {
        using var admin = Admin();
        var logo = await UploadAsync(admin, "/clinic/logo");
        var tabIcon = await UploadAsync(admin, "/clinic/tab-icon");
        Assert.StartsWith("storage:clinic-brand/logo-", logo, StringComparison.Ordinal);
        Assert.StartsWith("storage:clinic-brand/tab-icon-", tabIcon, StringComparison.Ordinal);

        var saved = await OkAsync(await admin.PatchAsJsonAsync(
            "/clinic", new { logo_url = logo, tab_icon_url = tabIcon, show_name_with_logo = false }, Token));
        Assert.Equal(logo, saved.GetProperty("logo_url").GetString());
        Assert.Equal(tabIcon, saved.GetProperty("tab_icon_url").GetString());
        Assert.False(saved.GetProperty("show_name_with_logo").GetBoolean());

        using var anonymous = Anonymous();
        var profile = await anonymous.GetFromJsonAsync<JsonElement>("/clinic", Token);
        Assert.Equal(logo, profile.GetProperty("logo_url").GetString());
        Assert.Equal(tabIcon, profile.GetProperty("tab_icon_url").GetString());
        Assert.False(profile.GetProperty("show_name_with_logo").GetBoolean());

        await AssertServedAsync("/clinic/logo");
        await AssertServedAsync("/clinic/tab-icon");

        // Sending the stored values back with another change keeps both, and the switch stays off.
        var kept = await OkAsync(await admin.PatchAsJsonAsync(
            "/clinic", new { logo_url = logo, tab_icon_url = tabIcon, license_no = "สพ. 50/2569" }, Token));
        Assert.Equal(logo, kept.GetProperty("logo_url").GetString());
        Assert.Equal(tabIcon, kept.GetProperty("tab_icon_url").GetString());
        Assert.False(kept.GetProperty("show_name_with_logo").GetBoolean());
    }

    [Fact]
    public async Task Clearing_goes_back_to_the_paw_and_the_tab_icon_does_not_fall_back_to_the_logo()
    {
        using var admin = Admin();
        var logo = await UploadAsync(admin, "/clinic/logo");
        var tabIcon = await UploadAsync(admin, "/clinic/tab-icon");
        await OkAsync(await admin.PatchAsJsonAsync("/clinic", new { logo_url = logo, tab_icon_url = tabIcon }, Token));

        var noTabIcon = await OkAsync(await admin.PatchAsJsonAsync("/clinic", new { tab_icon_url = "" }, Token));
        Assert.False(noTabIcon.TryGetProperty("tab_icon_url", out _));

        using var anonymous = Anonymous();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/clinic/tab-icon", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Found, (await anonymous.GetAsync("/clinic/logo", Token)).StatusCode);

        var cleared = await OkAsync(await admin.PatchAsJsonAsync("/clinic", new { logo_url = "", show_name_with_logo = true }, Token));
        Assert.False(cleared.TryGetProperty("logo_url", out _));
        Assert.True(cleared.GetProperty("show_name_with_logo").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/clinic/logo", Token)).StatusCode);
    }

    [Fact]
    public async Task A_web_address_still_works_and_is_not_served_through_the_logo_route()
    {
        using var admin = Admin();

        var saved = await OkAsync(await admin.PatchAsJsonAsync("/clinic", new { logo_url = "https://cdn.happypaws.test/logo.png" }, Token));
        Assert.Equal("https://cdn.happypaws.test/logo.png", saved.GetProperty("logo_url").GetString());

        // The website uses that address as it is; redirecting to it would send people wherever was typed.
        using var anonymous = Anonymous();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/clinic/logo", Token)).StatusCode);
    }

    [Theory]
    [InlineData("/clinic/logo", "logo_url")]
    [InlineData("/clinic/tab-icon", "tab_icon_url")]
    public async Task A_picture_whose_file_never_arrived_is_refused(string route, string field)
    {
        using var admin = Admin();

        var started = await OkAsync(await admin.PostAsJsonAsync(route, new { content_type = "image/jpeg", size_bytes = 100 }, Token));

        var missing = await admin.PatchAsJsonAsync(
            "/clinic", new Dictionary<string, string?> { [field] = started.GetProperty("value").GetString() }, Token);

        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        Assert.Equal("invalid_clinic_profile", (await ErrorAsync(missing)).GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("logo_url", "storage:staff-photos/stf_1/photo-" + Hex + ".jpg")]
    [InlineData("logo_url", "storage:articles/art_1/cover-" + Hex + ".jpg")]
    [InlineData("logo_url", "storage:clinic-brand/logo-" + Hex + ".svg")]
    [InlineData("logo_url", "storage:clinic-brand/tab-icon-" + Hex + ".png")]
    [InlineData("logo_url", "storage:clinic-brand/../staff-photos/stf_1/photo-" + Hex + ".jpg")]
    [InlineData("tab_icon_url", "storage:clinic-brand/logo-" + Hex + ".png")]
    [InlineData("tab_icon_url", "https://cdn.happypaws.test/icon.png")]
    public async Task A_key_from_elsewhere_or_in_the_wrong_shape_is_refused(string field, string value)
    {
        using var admin = Admin();

        var response = await admin.PatchAsJsonAsync("/clinic", new Dictionary<string, string> { [field] = value }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(field, (await ErrorAsync(response)).GetProperty("field").GetString());
    }
}
