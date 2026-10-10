using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Articles;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Articles;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Articles;

/// <summary>
/// <c>docs/contract-extensions.md § 26</c>: staff write, publish, take down and delete the health
/// articles, and the public site only ever sees what is published.
/// <para>
/// The suite shares one database and the public list shows the newest 50, so each test publishes
/// as little as it can and every article has a slug of its own.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class StaffArticlesTests(ApiFixture fixture)
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 9, 8, 7, 6];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Editor() => fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Content.Manage]));

    private static string UniqueSlug(string stem) => $"{stem}-{Guid.NewGuid().ToString("N")[..10]}";

    private static object NewDraft(string? slug, string title = "Dog dental care", string body = "ย่อหน้าแรก\n\nย่อหน้าที่สอง") => new
    {
        title,
        slug,
        summary = "ฟันสุนัขต้องดูแลอย่างไร",
        body,
        author_name = "สพ.ญ. มินท์",
    };

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(Token);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static async Task<JsonElement> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    private async Task<JsonElement> CreateAsync(HttpClient editor, string? slug, string title = "Dog dental care", string body = "ย่อหน้าแรก\n\nย่อหน้าที่สอง") =>
        await OkAsync(await editor.PostAsJsonAsync("/staff/articles", NewDraft(slug, title, body), Token));

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null, uint? ifMatch = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (ifMatch is { } version)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        }

        return await client.SendAsync(request, Token);
    }

    private static uint VersionOf(JsonElement article) => article.GetProperty("version").GetUInt32();

    private static string IdOf(JsonElement article) => article.GetProperty("id").GetString()!;

    private async Task<List<string>> PublicSlugsAsync()
    {
        using var anonymous = fixture.CreateClient();
        var list = await anonymous.GetFromJsonAsync<JsonElement>("/articles?limit=50", Token);

        return [.. list.GetProperty("data").EnumerateArray().Select(article => article.GetProperty("slug").GetString()!)];
    }

    private async Task<HttpStatusCode> PublicStatusAsync(string slug)
    {
        using var anonymous = fixture.CreateClient();
        return (await anonymous.GetAsync($"/articles/{slug}", Token)).StatusCode;
    }

    private async Task<List<AuditEntry>> AuditAsync(string articleId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.ResourceType == AuditResources.Article && entry.ResourceId == articleId)
            .OrderBy(entry => entry.CreatedAt)
            .ToListAsync(Token);
    }

    [Fact]
    public async Task Every_route_needs_content_manage_and_a_session()
    {
        using var anonymous = fixture.CreateClient();
        using var other = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Settings.Write, Scopes.Staff.Manage]));

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/articles", Token)).StatusCode);

        var refused = new[]
        {
            await other.GetAsync("/staff/articles", Token),
            await other.GetAsync("/staff/articles/art_x", Token),
            await other.PostAsJsonAsync("/staff/articles", NewDraft(UniqueSlug("refused")), Token),
            await SendAsync(other, HttpMethod.Patch, "/staff/articles/art_x", new { title = "x" }, 1),
            await other.PostAsync("/staff/articles/art_x/publish", null, Token),
            await other.PostAsync("/staff/articles/art_x/unpublish", null, Token),
            await other.DeleteAsync("/staff/articles/art_x", Token),
            await other.PostAsJsonAsync("/staff/articles/art_x/cover", new { content_type = "image/png", size_bytes = 10 }, Token),
        };

        Assert.All(refused, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        Assert.Equal("insufficient_scope", (await ErrorAsync(refused[0])).GetProperty("code").GetString());

        // The owner's wildcard covers the new scope without anyone granting it.
        using var owner = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Wildcard]));
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/staff/articles", Token)).StatusCode);
    }

    [Fact]
    public void The_starter_admin_role_is_given_the_scope_from_the_catalogue()
    {
        var admin = DefaultRoles.StarterScopes.Single(role => role.Key == DefaultRoles.ClinicAdmin).Value;

        Assert.Contains(Scopes.Content.Manage, admin);
        Assert.Contains(Scopes.Content.Manage, ScopeCatalogue.All);
    }

    [Fact]
    public async Task A_draft_is_checked_and_its_slug_is_made_from_an_english_title()
    {
        using var editor = Editor();

        var missing = await editor.PostAsJsonAsync("/staff/articles", new { title = "Only a title" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);

        var badSlug = await editor.PostAsJsonAsync("/staff/articles", NewDraft("วัคซีน-แมว"), Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badSlug.StatusCode);
        Assert.Equal("slug", (await ErrorAsync(badSlug)).GetProperty("field").GetString());

        // A Thai title has no latin letters to make an address from: the writer is asked for one.
        var thai = await editor.PostAsJsonAsync("/staff/articles", NewDraft(null, title: "ดูแลฟันสุนัขอย่างไร"), Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, thai.StatusCode);
        Assert.Equal("slug", (await ErrorAsync(thai)).GetProperty("field").GetString());

        var stem = Guid.NewGuid().ToString("N")[..8];
        var made = await CreateAsync(editor, null, title: $"Dog Dental Care {stem}");
        Assert.Equal($"dog-dental-care-{stem}", made.GetProperty("slug").GetString());
        Assert.Equal("draft", made.GetProperty("status").GetString());
        Assert.False(made.GetProperty("slug_locked").GetBoolean());
        Assert.Equal(1, made.GetProperty("reading_minutes").GetInt32());

        var typed = await CreateAsync(editor, $"  My Typed Slug {stem} ");
        Assert.Equal($"my-typed-slug-{stem}", typed.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task A_slug_held_by_another_article_is_refused_deleted_or_not()
    {
        using var editor = Editor();
        var slug = UniqueSlug("held");
        var holder = await CreateAsync(editor, slug);

        var clash = await editor.PostAsJsonAsync("/staff/articles", NewDraft(slug.ToUpperInvariant()), Token);
        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        var error = await ErrorAsync(clash);
        Assert.Equal("article_slug_taken", error.GetProperty("code").GetString());
        Assert.Equal(IdOf(holder), error.GetProperty("details").GetProperty("article_id").GetString());

        var other = await CreateAsync(editor, UniqueSlug("other"));
        var rename = await SendAsync(editor, HttpMethod.Patch, $"/staff/articles/{IdOf(other)}", new { slug }, VersionOf(other));
        Assert.Equal(HttpStatusCode.Conflict, rename.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await editor.DeleteAsync($"/staff/articles/{IdOf(holder)}", Token)).StatusCode);

        var afterDelete = await editor.PostAsJsonAsync("/staff/articles", NewDraft(slug), Token);
        Assert.Equal(HttpStatusCode.Conflict, afterDelete.StatusCode);
        Assert.True((await ErrorAsync(afterDelete)).GetProperty("details").GetProperty("deleted").GetBoolean());

        // The seed's own check counts the deleted article too, so it never brings one back.
        using var scope = fixture.CreateScope();
        var taken = await scope.ServiceProvider.GetRequiredService<IArticleRepository>().ExistingSlugsAsync([slug], Token);
        Assert.Contains(slug, taken);
    }

    [Fact]
    public async Task An_edit_needs_the_version_it_was_read_at()
    {
        using var editor = Editor();
        var article = await CreateAsync(editor, UniqueSlug("versioned"));
        var path = $"/staff/articles/{IdOf(article)}";

        var blind = await SendAsync(editor, HttpMethod.Patch, path, new { title = "ไม่มี If-Match" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blind.StatusCode);
        Assert.Equal("If-Match", (await ErrorAsync(blind)).GetProperty("field").GetString());

        var edited = await OkAsync(await SendAsync(editor, HttpMethod.Patch, path, new { title = "ชื่อใหม่", reading_minutes = 7 }, VersionOf(article)));
        Assert.Equal("ชื่อใหม่", edited.GetProperty("title").GetString());
        Assert.Equal(7, edited.GetProperty("reading_minutes").GetInt32());

        var stale = await SendAsync(editor, HttpMethod.Patch, path, new { title = "ทับของคนอื่น" }, VersionOf(article));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("version_conflict", (await ErrorAsync(stale)).GetProperty("code").GetString());

        var bad = await SendAsync(editor, HttpMethod.Patch, path, new { body = "   " }, VersionOf(edited));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(editor, HttpMethod.Patch, "/staff/articles/art_missing", new { title = "x" }, 1)).StatusCode);
    }

    [Fact]
    public async Task A_draft_is_never_public_and_publishing_and_taking_down_show_on_the_public_reads()
    {
        using var editor = Editor();
        var slug = UniqueSlug("lifecycle");
        var draft = await CreateAsync(editor, slug);
        var id = IdOf(draft);

        Assert.Equal(HttpStatusCode.NotFound, await PublicStatusAsync(slug));
        Assert.DoesNotContain(slug, await PublicSlugsAsync());

        var published = await OkAsync(await SendAsync(editor, HttpMethod.Post, $"/staff/articles/{id}/publish", ifMatch: VersionOf(draft)));
        Assert.Equal("published", published.GetProperty("status").GetString());
        Assert.True(published.GetProperty("slug_locked").GetBoolean());
        var firstDate = published.GetProperty("published_at").GetDateTimeOffset();

        Assert.Equal(HttpStatusCode.OK, await PublicStatusAsync(slug));
        Assert.Contains(slug, await PublicSlugsAsync());

        var twice = await editor.PostAsync($"/staff/articles/{id}/publish", null, Token);
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Equal("state_conflict", (await ErrorAsync(twice)).GetProperty("code").GetString());

        var down = await OkAsync(await editor.PostAsync($"/staff/articles/{id}/unpublish", null, Token));
        Assert.Equal("draft", down.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound, await PublicStatusAsync(slug));
        Assert.DoesNotContain(slug, await PublicSlugsAsync());
        Assert.Equal(HttpStatusCode.Conflict, (await editor.PostAsync($"/staff/articles/{id}/unpublish", null, Token)).StatusCode);

        // Once out in the world, the address stays, even while the article is down.
        var move = await SendAsync(editor, HttpMethod.Patch, $"/staff/articles/{id}", new { slug = UniqueSlug("moved") }, VersionOf(down));
        Assert.Equal(HttpStatusCode.Conflict, move.StatusCode);
        Assert.Equal("state_conflict", (await ErrorAsync(move)).GetProperty("code").GetString());

        var again = await OkAsync(await editor.PostAsync($"/staff/articles/{id}/publish", null, Token));
        // The first date comes back, give or take the database's microsecond precision.
        Assert.Equal(firstDate, again.GetProperty("published_at").GetDateTimeOffset(), TimeSpan.FromMilliseconds(1));

        var stale = await SendAsync(editor, HttpMethod.Post, $"/staff/articles/{id}/unpublish", ifMatch: VersionOf(down));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("version_conflict", (await ErrorAsync(stale)).GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await editor.DeleteAsync($"/staff/articles/{id}", Token)).StatusCode);
    }

    [Fact]
    public async Task An_edit_to_a_published_article_shows_on_the_public_page()
    {
        using var editor = Editor();
        var slug = UniqueSlug("live-edit");
        var article = await CreateAsync(editor, slug);
        var published = await OkAsync(await editor.PostAsync($"/staff/articles/{IdOf(article)}/publish", null, Token));

        await OkAsync(await SendAsync(editor, HttpMethod.Patch, $"/staff/articles/{IdOf(article)}", new { body = "เนื้อหาที่แก้แล้ว" }, VersionOf(published)));

        using var anonymous = fixture.CreateClient();
        var page = await anonymous.GetFromJsonAsync<JsonElement>($"/articles/{slug}", Token);
        Assert.Equal("เนื้อหาที่แก้แล้ว", page.GetProperty("body").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await editor.DeleteAsync($"/staff/articles/{IdOf(article)}", Token)).StatusCode);
    }

    [Fact]
    public async Task Deleting_takes_a_published_article_off_the_site_and_out_of_every_staff_read()
    {
        using var editor = Editor();
        var slug = UniqueSlug("deleted");
        var article = await CreateAsync(editor, slug);
        var id = IdOf(article);
        var published = await OkAsync(await editor.PostAsync($"/staff/articles/{id}/publish", null, Token));
        Assert.Equal(HttpStatusCode.OK, await PublicStatusAsync(slug));

        var stale = await SendAsync(editor, HttpMethod.Delete, $"/staff/articles/{id}", ifMatch: VersionOf(article));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(editor, HttpMethod.Delete, $"/staff/articles/{id}", ifMatch: VersionOf(published))).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, await PublicStatusAsync(slug));
        Assert.DoesNotContain(slug, await PublicSlugsAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await editor.GetAsync($"/staff/articles/{id}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await editor.DeleteAsync($"/staff/articles/{id}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await editor.PostAsync($"/staff/articles/{id}/publish", null, Token)).StatusCode);

        var list = await editor.GetFromJsonAsync<JsonElement>($"/staff/articles?q={slug}", Token);
        Assert.Equal(0, list.GetProperty("data").GetArrayLength());

        // The row stays, so the address stays taken and the audit rows still point at something.
        using var scope = fixture.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Set<Article>().AsNoTracking()
            .SingleAsync(entry => entry.Id == id, Token);
        Assert.NotNull(row.DeletedAt);
        Assert.Equal("stf_test", row.DeletedByStaffId);
        Assert.Null(row.PublishedAt);
    }

    [Fact]
    public async Task The_staff_list_shows_drafts_filters_by_status_and_searches()
    {
        using var editor = Editor();
        var stem = Guid.NewGuid().ToString("N")[..10];
        var draft = await CreateAsync(editor, $"list-draft-{stem}");
        var live = await CreateAsync(editor, $"list-live-{stem}");
        await OkAsync(await editor.PostAsync($"/staff/articles/{IdOf(live)}/publish", null, Token));

        async Task<List<string>> SlugsAsync(string query) =>
            [.. (await editor.GetFromJsonAsync<JsonElement>($"/staff/articles?{query}", Token))
                .GetProperty("data").EnumerateArray().Select(row => row.GetProperty("slug").GetString()!)];

        var all = await SlugsAsync($"q={stem}");
        Assert.Equal([$"list-live-{stem}", $"list-draft-{stem}"], all);

        Assert.Equal([$"list-draft-{stem}"], await SlugsAsync($"q={stem}&status=draft"));
        Assert.Equal([$"list-live-{stem}"], await SlugsAsync($"q={stem}&status=published"));
        Assert.Empty(await SlugsAsync($"q={stem}&status=scheduled"));

        var row = (await editor.GetFromJsonAsync<JsonElement>($"/staff/articles?q={stem}&status=draft", Token)).GetProperty("data")[0];
        Assert.False(row.TryGetProperty("body", out _));

        var paged = await editor.GetFromJsonAsync<JsonElement>($"/staff/articles?q={stem}&limit=1", Token);
        var next = paged.GetProperty("page").GetProperty("next_cursor").GetString();
        Assert.Equal([$"list-draft-{stem}"], await SlugsAsync($"q={stem}&limit=1&cursor={next}"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await editor.GetAsync("/staff/articles?status=hidden", Token)).StatusCode);

        var one = await editor.GetAsync($"/staff/articles/{IdOf(draft)}", Token);
        Assert.Equal($"\"{VersionOf(draft)}\"", one.Headers.ETag?.Tag);
        Assert.Equal("ย่อหน้าแรก\n\nย่อหน้าที่สอง", (await OkAsync(one)).GetProperty("body").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await editor.DeleteAsync($"/staff/articles/{IdOf(live)}", Token)).StatusCode);
    }

    [Fact]
    public async Task Script_in_an_article_is_kept_as_text_and_never_reaches_the_site_as_markup()
    {
        using var editor = Editor();
        var slug = UniqueSlug("xss");
        var rlo = char.ConvertFromUtf32(0x202E);
        const string script = "<script>alert(document.cookie)</script>";
        const string image = "<img src=x onerror=alert(1)>";

        var article = await CreateAsync(editor, slug, title: $"Pets {image}", body: $"{script}\0\n\n{rlo}gnp.exe {image}");
        await OkAsync(await editor.PostAsync($"/staff/articles/{IdOf(article)}/publish", null, Token));

        using var anonymous = fixture.CreateClient();
        var response = await anonymous.GetAsync($"/articles/{slug}", Token);

        // JSON, never HTML: nothing on this route is ever parsed as a page.
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var page = await OkAsync(response);
        var body = page.GetProperty("body").GetString()!;

        // The markup is the characters typed, for the site to show as text; what has no place in
        // text (the NUL, the right-to-left override) is gone.
        Assert.Equal($"{script}\n\ngnp.exe {image}", body);
        Assert.Equal($"Pets {image}", page.GetProperty("title").GetString());
        Assert.DoesNotContain('\0', body);
        Assert.DoesNotContain(rlo, body, StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.NoContent, (await editor.DeleteAsync($"/staff/articles/{IdOf(article)}", Token)).StatusCode);
    }

    [Fact]
    public async Task Every_change_is_audited_and_an_edit_that_changes_nothing_is_not()
    {
        using var editor = Editor();
        var article = await CreateAsync(editor, UniqueSlug("audited"));
        var id = IdOf(article);

        var edited = await OkAsync(await SendAsync(editor, HttpMethod.Patch, $"/staff/articles/{id}", new { summary = "คำโปรยใหม่" }, VersionOf(article)));
        await OkAsync(await SendAsync(editor, HttpMethod.Patch, $"/staff/articles/{id}", new { summary = "คำโปรยใหม่" }, VersionOf(edited)));
        await OkAsync(await editor.PostAsync($"/staff/articles/{id}/publish", null, Token));
        await OkAsync(await editor.PostAsync($"/staff/articles/{id}/unpublish", null, Token));
        Assert.Equal(HttpStatusCode.NoContent, (await editor.DeleteAsync($"/staff/articles/{id}", Token)).StatusCode);

        var entries = await AuditAsync(id);

        Assert.Equal(
            [AuditActions.ArticleCreated, AuditActions.ArticleUpdated, AuditActions.ArticlePublished, AuditActions.ArticleUnpublished, AuditActions.ArticleDeleted],
            entries.Select(entry => entry.Action));
        Assert.All(entries, entry => Assert.Equal("stf_test", entry.ActorStaffId));
        Assert.Contains("summary", entries[1].DetailsJson);
    }

    [Fact]
    public async Task A_cover_is_uploaded_through_its_signed_url_and_shown_only_while_the_article_is_published()
    {
        using var editor = Editor();
        var slug = UniqueSlug("cover");
        var article = await CreateAsync(editor, slug);
        var id = IdOf(article);

        var svg = await editor.PostAsJsonAsync($"/staff/articles/{id}/cover", new { content_type = "image/svg+xml", size_bytes = 100 }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, svg.StatusCode);
        var huge = await editor.PostAsJsonAsync($"/staff/articles/{id}/cover", new { content_type = "image/png", size_bytes = 6 * 1024 * 1024 }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, huge.StatusCode);

        var started = await OkAsync(await editor.PostAsJsonAsync($"/staff/articles/{id}/cover", new { content_type = "image/png", size_bytes = Png.Length }, Token));
        Assert.True(started.GetProperty("article").GetProperty("cover_pending").GetBoolean());
        var upload = started.GetProperty("upload");
        Assert.Equal("PUT", upload.GetProperty("method").GetString());

        using (var anyone = fixture.CreateClient())
        using (var content = new ByteArrayContent(Png))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            var put = await anyone.PutAsync(new Uri(upload.GetProperty("url").GetString()!).PathAndQuery, content, Token);
            Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        }

        var withCover = await OkAsync(await editor.GetAsync($"/staff/articles/{id}", Token));
        Assert.False(withCover.GetProperty("cover_pending").GetBoolean());
        Assert.True(withCover.TryGetProperty("cover_image_url", out _));

        // A draft's cover has no link anywhere a stranger can reach.
        Assert.Equal(HttpStatusCode.NotFound, await PublicStatusAsync(slug));

        await OkAsync(await editor.PostAsync($"/staff/articles/{id}/publish", null, Token));

        using (var anonymous = fixture.CreateClient())
        {
            var page = await anonymous.GetFromJsonAsync<JsonElement>($"/articles/{slug}", Token);
            var link = page.GetProperty("cover_image_url").GetString()!;

            var image = await anonymous.GetAsync(new Uri(link).PathAndQuery, Token);
            Assert.Equal(HttpStatusCode.OK, image.StatusCode);
            Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync(Token));

            var list = await anonymous.GetFromJsonAsync<JsonElement>("/articles?limit=50", Token);
            var card = list.GetProperty("data").EnumerateArray().Single(row => row.GetProperty("slug").GetString() == slug);
            Assert.True(card.TryGetProperty("cover_image_url", out _));
        }

        var current = await OkAsync(await editor.GetAsync($"/staff/articles/{id}", Token));
        var removed = await OkAsync(await SendAsync(editor, HttpMethod.Patch, $"/staff/articles/{id}", new { remove_cover = true }, VersionOf(current)));
        Assert.False(removed.TryGetProperty("cover_image_url", out _));

        Assert.Equal(HttpStatusCode.NoContent, (await editor.DeleteAsync($"/staff/articles/{id}", Token)).StatusCode);
    }
}
