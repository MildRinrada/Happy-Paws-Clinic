using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>
/// "อัปโหลดผ่าน pre-signed URL · เลือกให้เจ้าของเห็นได้" and "อัปโหลดรูปอาการหรือผลจากคลินิกอื่น",
/// end to end: ask for a URL, send the bytes there, read them back.
/// </summary>
[Collection(nameof(ApiCollection))]
public class DocumentUploadTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    /// <summary>The path of a storage URL, which the test host serves however the URL was spelled.</summary>
    private static string PathOf(string url) => new Uri(url).PathAndQuery;

    private async Task<HttpResponseMessage> PutAsync(string url, byte[] bytes, string contentType)
    {
        using var client = fixture.CreateClient();
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return await client.PutAsync(PathOf(url), content, Token);
    }

    [Fact]
    public async Task A_clinic_document_is_uploaded_through_its_url_and_downloads_unchanged()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.StaffOf(clinic, Scopes.Record.Write);

        var response = await vet.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/documents",
            new { file_name = "xray.png", content_type = "image/png", size_bytes = Png.Length, kind = "imaging", visible_to_owner = true },
            Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var started = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var upload = started.GetProperty("upload");

        Assert.Equal("PUT", upload.GetProperty("method").GetString());
        Assert.False(started.GetProperty("document").TryGetProperty("download_url", out _));

        var put = await PutAsync(upload.GetProperty("url").GetString()!, Png, "image/png");
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        using var customer = fixture.CustomerOf(clinic);
        await PortalScenario.OpenSectionsAsync(fixture, ["attachments"], Token);

        try
        {
            var records = await customer.GetFromJsonAsync<JsonElement>($"/portal/pets/{clinic.PetId}/records", Token);
            var link = records.GetProperty("attachments")[0].GetProperty("download_url").GetString()!;

            using var anyone = fixture.CreateClient();
            Assert.Equal(Png, await anyone.GetByteArrayAsync(PathOf(link), Token));
        }
        finally
        {
            await PortalScenario.OpenSectionsAsync(fixture, [], Token);
        }
    }

    [Fact]
    public async Task An_upload_larger_than_declared_or_of_another_type_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.StaffOf(clinic, Scopes.Record.Write);

        var started = await (await vet.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/documents",
            new { file_name = "scan.png", content_type = "image/png", size_bytes = 4 },
            Token)).Content.ReadFromJsonAsync<JsonElement>(Token);

        var url = started.GetProperty("upload").GetProperty("url").GetString()!;

        Assert.Equal(HttpStatusCode.Forbidden, (await PutAsync(url, Png, "image/png")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PutAsync(url, [1, 2], "application/pdf")).StatusCode);
    }

    [Fact]
    public async Task A_tampered_upload_url_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.StaffOf(clinic, Scopes.Record.Write);

        var started = await (await vet.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/documents",
            new { file_name = "scan.png", content_type = "image/png", size_bytes = 100 },
            Token)).Content.ReadFromJsonAsync<JsonElement>(Token);

        var url = started.GetProperty("upload").GetProperty("url").GetString()!;

        // One character changed in what the URL grants: the signature no longer matches.
        Assert.Equal(HttpStatusCode.Forbidden, (await PutAsync(url.Replace("/uploads/", "/uploads/e", StringComparison.Ordinal), Png, "image/png")).StatusCode);
    }

    [Fact]
    public async Task Only_images_and_pdfs_are_accepted()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var vet = fixture.StaffOf(clinic, Scopes.Record.Write);

        var response = await vet.PostAsJsonAsync(
            $"/staff/pets/{clinic.PetId}/documents",
            new { file_name = "run.exe", content_type = "application/octet-stream", size_bytes = 100 },
            Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task An_owner_uploads_for_their_own_pet_and_not_for_anyone_elses()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (_, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var customer = fixture.CustomerOf(clinic);

        object Body(string petId) => new { pet_id = petId, file_name = "rash.jpg", content_type = "image/jpeg", size_bytes = 2048, kind = "photo" };

        var mine = await customer.PostAsJsonAsync("/portal/documents", Body(clinic.PetId), Token);
        var theirs = await customer.PostAsJsonAsync("/portal/documents", Body(otherPet), Token);

        Assert.Equal(HttpStatusCode.Created, mine.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, theirs.StatusCode);

        var document = (await mine.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("document");
        Assert.Equal("owner", document.GetProperty("source").GetString());
        Assert.True(document.GetProperty("visible_to_owner").GetBoolean());
    }
}
