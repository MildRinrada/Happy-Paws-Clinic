using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Clinics;

/// <summary>
/// The clinic's contact entries — a contract extension, <c>docs/contract-extensions.md § 15</c>.
/// <para>
/// The list is the whole clinic's and other tests share the database, so each test finds its own
/// entries by id and removes them when it is done, rather than asserting on the whole list.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class ClinicContactTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Admin(params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, scopes: scopes.Length == 0 ? [Scopes.Settings.Read, Scopes.Settings.Write] : scopes));

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Token);

    private async Task<JsonElement> PublicListAsync()
    {
        using var anyone = fixture.CreateClient();
        var list = await anyone.GetFromJsonAsync<JsonElement>("/clinic/contacts", Token);

        return list.GetProperty("data");
    }

    [Fact]
    public async Task An_entry_is_added_shown_publicly_changed_and_removed()
    {
        using var admin = Admin();
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        var added = await admin.PostAsJsonAsync(
            "/clinic/contacts",
            new { kind = "phone", label = "โทรศัพท์สาขา", value = "02-555-0101", branch_id = clinic.BranchId, sort_order = 5 },
            Token);

        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var entry = await BodyAsync(added);
        var id = entry.GetProperty("id").GetString()!;

        Assert.StartsWith("cct_", id);
        Assert.Equal(clinic.BranchId, entry.GetProperty("branch_id").GetString());

        // The website reads it without signing in.
        Assert.Contains((await PublicListAsync()).EnumerateArray(), row => row.GetProperty("id").GetString() == id);

        var changed = await admin.PatchAsJsonAsync(
            $"/clinic/contacts/{id}", new { value = "02-555-0199", branch_id = "" }, Token);
        changed.EnsureSuccessStatusCode();
        var after = await BodyAsync(changed);

        // An absent field is left alone; an empty string clears an optional one.
        Assert.Equal("02-555-0199", after.GetProperty("value").GetString());
        Assert.Equal("โทรศัพท์สาขา", after.GetProperty("label").GetString());
        Assert.False(after.TryGetProperty("branch_id", out _));

        var removed = await admin.DeleteAsync($"/clinic/contacts/{id}", Token);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.DoesNotContain((await PublicListAsync()).EnumerateArray(), row => row.GetProperty("id").GetString() == id);

        var again = await admin.DeleteAsync($"/clinic/contacts/{id}", Token);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task The_list_comes_in_display_order()
    {
        using var admin = Admin();
        var ids = new List<string>();

        try
        {
            foreach (var (label, order) in new[] { ("ลำดับสาม", 30), ("ลำดับหนึ่ง", 10), ("ลำดับสอง", 20) })
            {
                var response = await admin.PostAsJsonAsync(
                    "/clinic/contacts", new { kind = "other", label, value = label, sort_order = order + 900_000 }, Token);
                ids.Add((await BodyAsync(response)).GetProperty("id").GetString()!);
            }

            var labels = (await PublicListAsync()).EnumerateArray()
                .Where(row => ids.Contains(row.GetProperty("id").GetString()!))
                .Select(row => row.GetProperty("label").GetString())
                .ToList();

            Assert.Equal(["ลำดับหนึ่ง", "ลำดับสอง", "ลำดับสาม"], labels);
        }
        finally
        {
            foreach (var id in ids) (await admin.DeleteAsync($"/clinic/contacts/{id}", Token)).Dispose();
        }
    }

    [Theory]
    [InlineData("pager", "x", null, null, "kind")]
    [InlineData("website", "เว็บ", "javascript:alert(1)", null, "url")]
    [InlineData("phone", "โทร", null, "https://www.google.com/maps/embed?pb=1", "embed_url")]
    [InlineData("map", "แผนที่", null, "https://evil.example/embed", "embed_url")]
    public async Task A_malformed_entry_is_refused_on_its_field(string kind, string label, string? url, string? embedUrl, string field)
    {
        using var admin = Admin();

        var response = await admin.PostAsJsonAsync(
            "/clinic/contacts", new { kind, label, value = "ค่า", url, embed_url = embedUrl }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await BodyAsync(response)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task A_map_keeps_its_embed_address()
    {
        using var admin = Admin();
        const string embed = "https://www.google.com/maps/embed?pb=!1m18!test";

        var response = await admin.PostAsJsonAsync(
            "/clinic/contacts",
            new { kind = "map", label = "แผนที่", value = "สุขุมวิท 39", url = "https://maps.google.com/?q=x", embed_url = embed },
            Token);
        var entry = await BodyAsync(response);

        try
        {
            Assert.Equal(embed, entry.GetProperty("embed_url").GetString());

            // Changing a map into something else while it still carries an embed is refused.
            var refused = await admin.PatchAsJsonAsync($"/clinic/contacts/{entry.GetProperty("id").GetString()}", new { kind = "phone" }, Token);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        }
        finally
        {
            (await admin.DeleteAsync($"/clinic/contacts/{entry.GetProperty("id").GetString()}", Token)).Dispose();
        }
    }

    [Fact]
    public async Task Writing_needs_settings_write_and_a_stale_version_is_refused()
    {
        using var admin = Admin();
        using var reader = Admin(Scopes.Settings.Read);
        using var anyone = fixture.CreateClient();

        var body = new { kind = "email", label = "อีเมล", value = "hello@example.test" };

        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync("/clinic/contacts", body, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anyone.PostAsJsonAsync("/clinic/contacts", body, Token)).StatusCode);

        var entry = await BodyAsync(await admin.PostAsJsonAsync("/clinic/contacts", body, Token));
        var id = entry.GetProperty("id").GetString()!;
        var version = entry.GetProperty("version").GetUInt32();

        try
        {
            (await admin.PatchAsJsonAsync($"/clinic/contacts/{id}", new { label = "อีเมลคลินิก" }, Token)).EnsureSuccessStatusCode();

            using var stale = new HttpRequestMessage(HttpMethod.Patch, $"/clinic/contacts/{id}")
            {
                Content = JsonContent.Create(new { label = "เก่า" }),
            };
            stale.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");

            Assert.Equal(HttpStatusCode.Conflict, (await admin.SendAsync(stale, Token)).StatusCode);
        }
        finally
        {
            (await admin.DeleteAsync($"/clinic/contacts/{id}", Token)).Dispose();
        }
    }

    [Fact]
    public async Task An_entry_for_a_branch_that_does_not_exist_is_refused()
    {
        using var admin = Admin();

        var response = await admin.PostAsJsonAsync(
            "/clinic/contacts", new { kind = "phone", label = "โทร", value = "0", branch_id = "brn_nowhere" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
