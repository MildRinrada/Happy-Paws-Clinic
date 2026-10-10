using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Staff;

/// <summary>
/// <c>docs/contract-extensions.md § 32</c>: a member's photo for the team page is uploaded through
/// a signed URL, named in the profile by a <c>storage:</c> value, and shown to the public only as a
/// signed link minted for someone the website lists.
/// <para>
/// <c>GET /team</c> lists every clinic's people in the shared database, so each test looks only
/// for the member it made.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class StaffPhotoTests(ApiFixture fixture)
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("code").GetString();

    /// <summary>A manager and one accepted member at a fresh clinic.</summary>
    private async Task<(HttpClient Manager, string StaffId)> ArrangeAsync()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var manager = fixture.Manager(clinic, Scopes.Queue.Read);
        var (_, roleName) = await StaffScenario.CreateRoleAsync(manager, [Scopes.Queue.Read], Token);

        var member = await StaffScenario.InviteAndAcceptAsync(
            fixture, manager, StaffScenario.NewEmail("photo"), [roleName], [clinic.BranchId], Token);

        return (manager, member.StaffId);
    }

    /// <summary>Asks for an upload URL and sends the file to it; returns the profile value that names it.</summary>
    private async Task<string> UploadAsync(HttpClient manager, string staffId)
    {
        var started = await OkAsync(await manager.PostAsJsonAsync(
            $"/staff/members/{staffId}/public-profile/photo", new { content_type = "image/png", size_bytes = Png.Length }, Token));

        var upload = started.GetProperty("upload");
        Assert.Equal("PUT", upload.GetProperty("method").GetString());

        using var anyone = fixture.CreateClient();
        using var content = new ByteArrayContent(Png);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var put = await anyone.PutAsync(new Uri(upload.GetProperty("url").GetString()!).PathAndQuery, content, Token);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        return started.GetProperty("photo_path").GetString()!;
    }

    private Task<HttpResponseMessage> PutProfileAsync(HttpClient manager, string staffId, object profile) =>
        manager.PutAsJsonAsync($"/staff/members/{staffId}/public-profile", profile, Token);

    private async Task<(string Body, JsonElement? Member)> PublicTeamAsync(string staffId)
    {
        using var anonymous = fixture.CreateClient();
        var body = await anonymous.GetStringAsync("/team", Token);

        var member = JsonDocument.Parse(body).RootElement.GetProperty("data").EnumerateArray()
            .SelectMany(department => department.GetProperty("members").EnumerateArray())
            .Where(entry => entry.GetProperty("id").GetString() == staffId)
            .Select(entry => (JsonElement?)entry.Clone())
            .SingleOrDefault();

        return (body, member);
    }

    [Fact]
    public async Task Asking_for_an_upload_needs_staff_manage()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.StaffOf(clinic, Scopes.Staff.Read);

        var response = await reader.PostAsJsonAsync(
            $"/staff/members/{clinic.StaffId}/public-profile/photo", new { content_type = "image/png", size_bytes = 10 }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Nobody_uploads_a_photo_of_someone_who_holds_more_than_they_do()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var senior = fixture.Manager(clinic, Scopes.Queue.Read, Scopes.Record.Sign);
        using var junior = fixture.Manager(clinic, Scopes.Queue.Read);

        var (_, signingRole) = await StaffScenario.CreateRoleAsync(senior, [Scopes.Record.Sign], Token);
        var vet = await StaffScenario.InviteAndAcceptAsync(
            fixture, senior, StaffScenario.NewEmail(), [signingRole], [clinic.BranchId], Token);

        var refused = await junior.PostAsJsonAsync(
            $"/staff/members/{vet.StaffId}/public-profile/photo", new { content_type = "image/png", size_bytes = 10 }, Token);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);

        var allowed = await senior.PostAsJsonAsync(
            $"/staff/members/{vet.StaffId}/public-profile/photo", new { content_type = "image/png", size_bytes = 10 }, Token);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Theory]
    [InlineData("image/svg+xml", 100)]
    [InlineData("image/gif", 100)]
    [InlineData("text/html", 100)]
    [InlineData("image/png", 0)]
    [InlineData("image/png", 6 * 1024 * 1024)]
    public async Task Only_a_jpeg_png_or_webp_of_at_most_5_mb_is_accepted(string contentType, long sizeBytes)
    {
        var (manager, staffId) = await ArrangeAsync();
        using var owned = manager;

        var response = await manager.PostAsJsonAsync(
            $"/staff/members/{staffId}/public-profile/photo", new { content_type = contentType, size_bytes = sizeBytes }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_uploaded_photo_shows_on_the_team_page_as_a_signed_link()
    {
        var (manager, staffId) = await ArrangeAsync();
        using var owned = manager;

        var photoPath = await UploadAsync(manager, staffId);
        Assert.StartsWith($"storage:staff-photos/{staffId}/photo-", photoPath, StringComparison.Ordinal);

        var saved = await OkAsync(await PutProfileAsync(manager, staffId, new { job_title = "สัตวแพทย์ประจำ", photo_path = photoPath, show_on_website = true }));
        var profile = saved.GetProperty("public_profile");
        Assert.Equal(photoPath, profile.GetProperty("photo_path").GetString());
        Assert.True(profile.TryGetProperty("photo_url", out _));

        var (body, member) = await PublicTeamAsync(staffId);
        Assert.NotNull(member);
        Assert.False(member.Value.TryGetProperty("photo_path", out _));
        Assert.DoesNotContain("staff-photos", body, StringComparison.Ordinal);

        using var anonymous = fixture.CreateClient();
        var image = await anonymous.GetAsync(new Uri(member.Value.GetProperty("photo_url").GetString()!).PathAndQuery, Token);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync(Token));

        // Sending the stored value back keeps the photo.
        var kept = await OkAsync(await PutProfileAsync(manager, staffId, new { job_title = "หัวหน้าสัตวแพทย์", photo_path = photoPath, show_on_website = true }));
        Assert.Equal(photoPath, kept.GetProperty("public_profile").GetProperty("photo_path").GetString());

        using var scope = fixture.CreateScope();
        var entries = await scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.ResourceType == AuditResources.StaffMember && entry.ResourceId == staffId
                && entry.Action == AuditActions.StaffPublicProfileUpdated)
            .ToListAsync(Token);

        Assert.Equal(3, entries.Count);
        Assert.Single(entries, entry => entry.DetailsJson!.Contains("photo_upload", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_hidden_profile_never_gives_its_photo_away()
    {
        var (manager, staffId) = await ArrangeAsync();
        using var owned = manager;

        var photoPath = await UploadAsync(manager, staffId);
        var saved = await OkAsync(await PutProfileAsync(manager, staffId, new { job_title = "สัตวแพทย์ประจำ", photo_path = photoPath, show_on_website = false }));

        // Staff still see it, to preview before turning the listing on.
        Assert.True(saved.GetProperty("public_profile").TryGetProperty("photo_url", out _));

        var (body, member) = await PublicTeamAsync(staffId);
        Assert.Null(member);
        Assert.DoesNotContain(staffId, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Leaving_the_photo_out_removes_it()
    {
        var (manager, staffId) = await ArrangeAsync();
        using var owned = manager;

        var photoPath = await UploadAsync(manager, staffId);
        await OkAsync(await PutProfileAsync(manager, staffId, new { photo_path = photoPath, show_on_website = true }));

        var removed = await OkAsync(await PutProfileAsync(manager, staffId, new { show_on_website = true }));
        Assert.False(removed.GetProperty("public_profile").TryGetProperty("photo_path", out _));
        Assert.False(removed.GetProperty("public_profile").TryGetProperty("photo_url", out _));

        var (_, member) = await PublicTeamAsync(staffId);
        Assert.NotNull(member);
        Assert.False(member.Value.TryGetProperty("photo_url", out _));
        Assert.False(member.Value.TryGetProperty("photo_path", out _));
    }

    [Fact]
    public async Task A_photo_whose_file_never_arrived_or_that_belongs_to_someone_else_is_refused()
    {
        var (manager, staffId) = await ArrangeAsync();
        using var owned = manager;

        var started = await OkAsync(await manager.PostAsJsonAsync(
            $"/staff/members/{staffId}/public-profile/photo", new { content_type = "image/jpeg", size_bytes = 100 }, Token));
        var neverSent = started.GetProperty("photo_path").GetString();

        var missing = await PutProfileAsync(manager, staffId, new { photo_path = neverSent, show_on_website = true });
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        Assert.Equal("invalid_public_profile", await ErrorCodeAsync(missing));

        var (otherManager, otherId) = await ArrangeAsync();
        using var ownedOther = otherManager;
        var othersPhoto = await UploadAsync(otherManager, otherId);

        var foreign = await PutProfileAsync(manager, staffId, new { photo_path = othersPhoto, show_on_website = true });
        Assert.Equal(HttpStatusCode.Conflict, foreign.StatusCode);
        Assert.Equal("invalid_public_profile", await ErrorCodeAsync(foreign));

        var malformed = await PutProfileAsync(manager, staffId, new { photo_path = "storage:staff-photos/../secrets.jpg", show_on_website = true });
        Assert.Equal(HttpStatusCode.Conflict, malformed.StatusCode);
    }

    [Fact]
    public async Task A_photo_under_the_websites_public_folder_still_works()
    {
        var (manager, staffId) = await ArrangeAsync();
        using var owned = manager;

        var saved = await OkAsync(await PutProfileAsync(manager, staffId, new { photo_path = "/images/team/mint.jpg", show_on_website = true }));
        Assert.Equal("/images/team/mint.jpg", saved.GetProperty("public_profile").GetProperty("photo_path").GetString());
        Assert.False(saved.GetProperty("public_profile").TryGetProperty("photo_url", out _));

        var (_, member) = await PublicTeamAsync(staffId);
        Assert.NotNull(member);
        Assert.Equal("/images/team/mint.jpg", member.Value.GetProperty("photo_path").GetString());
        Assert.False(member.Value.TryGetProperty("photo_url", out _));
    }
}
