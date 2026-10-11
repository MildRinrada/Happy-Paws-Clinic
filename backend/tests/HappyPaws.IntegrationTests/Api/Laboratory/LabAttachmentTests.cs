using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Api.Laboratory.LabScenario;

namespace HappyPaws.IntegrationTests.Api.Laboratory;

/// <summary>
/// <c>docs/contract-extensions.md § 54</c>: files from the lab go to a signed upload URL, are put on
/// the request by the value that names them, open through a five-minute link asked for each time,
/// and come off again, all within the lab's scopes and the request's branch.
/// </summary>
[Collection(nameof(ApiCollection))]
public class LabAttachmentTests(ApiFixture fixture)
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << >> endobj\ntrailer << >>\n%%EOF");
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Token);
    }

    private static Task<HttpResponseMessage> RequestUploadAsync(HttpClient client, string labId, string contentType, long size) =>
        client.PostAsJsonAsync($"/staff/lab-requests/{labId}/attachments/uploads", new { content_type = contentType, size_bytes = size }, Token);

    /// <summary>Asks for an upload URL and sends the bytes there; answers the value that names the file.</summary>
    private async Task<string> UploadAsync(HttpClient client, string labId, string contentType, byte[] bytes)
    {
        var started = await OkAsync(await RequestUploadAsync(client, labId, contentType, bytes.Length));
        var upload = started.GetProperty("upload");

        using var anyone = fixture.CreateClient();
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var put = await anyone.PutAsync(new Uri(upload.GetProperty("url").GetString()!).PathAndQuery, content, Token);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        return started.GetProperty("value").GetString()!;
    }

    private static Task<HttpResponseMessage> AttachAsync(HttpClient client, string labId, string value, string fileName = "ผล CBC.pdf", long? size = null) =>
        client.PostAsJsonAsync($"/staff/lab-requests/{labId}/attachments", new { file = value, file_name = fileName, size_bytes = size }, Token);

    [Fact]
    public async Task A_report_is_uploaded_attached_opened_as_a_download_and_removed()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token, "external");
        using var __ = lab;

        var value = await UploadAsync(lab, labId, "application/pdf", Pdf);
        Assert.StartsWith($"storage:lab-files/{labId}/file-", value, StringComparison.Ordinal);

        var attached = await OkAsync(await AttachAsync(lab, labId, value, "ผล CBC จาก Vet Central.pdf", Pdf.Length));
        var file = Assert.Single(attached.GetProperty("attachments").EnumerateArray());
        Assert.Equal("ผล CBC จาก Vet Central.pdf", file.GetProperty("file_name").GetString());
        Assert.Equal("application/pdf", file.GetProperty("content_type").GetString());
        Assert.Equal(Pdf.Length, file.GetProperty("size_bytes").GetInt64());
        var attachmentId = file.GetProperty("id").GetString()!;
        Assert.StartsWith("lbf_", attachmentId, StringComparison.Ordinal);

        // A file does not change whether the results have been read.
        Assert.Equal("pending", attached.GetProperty("status").GetString());

        // The worklist names the file but never hands out an address to it.
        var listed = await lab.GetStringAsync("/staff/lab-requests?status=pending", Token);
        Assert.Contains(attachmentId, listed, StringComparison.Ordinal);
        Assert.DoesNotContain("/storage/files/", listed, StringComparison.Ordinal);

        var linkResponse = await lab.GetAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}/link", Token);
        var link = await OkAsync(linkResponse);
        Assert.True(linkResponse.Headers.CacheControl?.NoStore);
        Assert.Contains("/storage/files/", link.GetProperty("url").GetString(), StringComparison.Ordinal);

        using var anyone = fixture.CreateClient();
        var download = await anyone.GetAsync(new Uri(link.GetProperty("url").GetString()!).PathAndQuery, Token);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("nosniff", download.Headers.GetValues("X-Content-Type-Options"));
        Assert.Equal(Pdf, await download.Content.ReadAsByteArrayAsync(Token));

        var removed = await OkAsync(await lab.DeleteAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}", Token));
        Assert.Empty(removed.GetProperty("attachments").EnumerateArray());

        Assert.Single(await AuditAsync(fixture, AuditActions.LabFileAttached, labId, Token));
        Assert.Single(await AuditAsync(fixture, AuditActions.LabFileOpened, labId, Token));
        var removal = Assert.Single(await AuditAsync(fixture, AuditActions.LabFileRemoved, labId, Token));
        Assert.Contains(value, removal.DetailsJson, StringComparison.Ordinal);

        var gone = await lab.GetAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}/link", Token);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task A_picture_opens_in_place()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;

        var attached = await OkAsync(await AttachAsync(lab, labId, await UploadAsync(lab, labId, "image/png", Png), "report.png"));
        var attachmentId = attached.GetProperty("attachments")[0].GetProperty("id").GetString();

        var link = await OkAsync(await lab.GetAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}/link", Token));
        using var anyone = fixture.CreateClient();
        var image = await anyone.GetAsync(new Uri(link.GetProperty("url").GetString()!).PathAndQuery, Token);

        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Null(image.Content.Headers.ContentDisposition);
        Assert.Contains("nosniff", image.Headers.GetValues("X-Content-Type-Options"));
    }

    [Theory]
    [InlineData("image/svg+xml", 100)]
    [InlineData("text/html", 100)]
    [InlineData("image/heic", 100)]
    [InlineData("application/pdf", 0)]
    [InlineData("application/pdf", 10 * 1024 * 1024 + 1)]
    public async Task Only_a_pdf_jpeg_png_or_webp_of_at_most_10_mb_is_accepted(string contentType, long sizeBytes)
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;

        var response = await RequestUploadAsync(lab, labId, contentType, sizeBytes);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_file_larger_than_asked_for_is_refused_by_the_storage()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var started = await OkAsync(await RequestUploadAsync(lab, labId, "application/pdf", 10));

        using var anyone = fixture.CreateClient();
        using var content = new ByteArrayContent(Pdf);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var put = await anyone.PutAsync(new Uri(started.GetProperty("upload").GetProperty("url").GetString()!).PathAndQuery, content, Token);

        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
    }

    [Fact]
    public async Task A_file_that_never_arrived_cannot_be_attached()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var started = await OkAsync(await RequestUploadAsync(lab, labId, "application/pdf", Pdf.Length));

        var response = await AttachAsync(lab, labId, started.GetProperty("value").GetString()!);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("invalid_lab_attachment", await ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task A_file_uploaded_for_another_request_cannot_be_attached()
    {
        var (clinic, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var value = await UploadAsync(lab, labId, "application/pdf", Pdf);

        var other = await (await lab.PostAsJsonAsync(
                "/staff/lab-requests",
                new { pet_id = clinic.PetId, branch_id = clinic.BranchId, destination = "in_house", tests = new[] { new { name = "UA" } } },
                Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        var response = await AttachAsync(lab, other.GetProperty("id").GetString()!, value);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("invalid_lab_attachment", await ErrorCodeAsync(response, Token));
    }

    [Theory]
    [InlineData("lab-files/lab_x/file-0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("storage:staff-photos/stf_1/photo-0123456789abcdef0123456789abcdef.png")]
    [InlineData("storage:lab-files/lab_x/../file-0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("storage:lab-files/lab_x/file-0123456789abcdef0123456789abcdef.svg")]
    public async Task A_value_the_upload_did_not_make_is_refused(string value)
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;

        var response = await AttachAsync(lab, labId, value);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Putting_files_on_needs_lab_write_and_opening_them_needs_lab_read()
    {
        var (clinic, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var attached = await OkAsync(await AttachAsync(lab, labId, await UploadAsync(lab, labId, "application/pdf", Pdf)));
        var attachmentId = attached.GetProperty("attachments")[0].GetProperty("id").GetString();

        using var reader = fixture.Vet(clinic, Scopes.Lab.Read, Scopes.Lab.Request);
        Assert.Equal(HttpStatusCode.Forbidden, (await RequestUploadAsync(reader, labId, "application/pdf", 10)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AttachAsync(reader, labId, "storage:x")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.DeleteAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}/link", Token)).StatusCode);

        using var technician = fixture.Vet(clinic, Scopes.Lab.Write);
        Assert.Equal(HttpStatusCode.Forbidden, (await technician.GetAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}/link", Token)).StatusCode);
    }

    [Fact]
    public async Task A_request_at_another_branch_is_not_found()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var value = await UploadAsync(lab, labId, "application/pdf", Pdf);
        var attached = await OkAsync(await AttachAsync(lab, labId, value));
        var attachmentId = attached.GetProperty("attachments")[0].GetProperty("id").GetString();
        using var elsewhere = Elsewhere(fixture);

        Assert.Equal(HttpStatusCode.NotFound, (await RequestUploadAsync(elsewhere, labId, "application/pdf", 10)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AttachAsync(elsewhere, labId, value)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await elsewhere.GetAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}/link", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await elsewhere.DeleteAsync($"/staff/lab-requests/{labId}/attachments/{attachmentId}", Token)).StatusCode);
    }

    [Fact]
    public async Task Attaching_against_a_version_already_changed_is_a_conflict()
    {
        var (_, lab, labId) = await RequestedAsync(fixture, Token);
        using var __ = lab;
        var first = await UploadAsync(lab, labId, "application/pdf", Pdf);
        var second = await UploadAsync(lab, labId, "image/png", Png);
        var before = await lab.GetFromJsonAsync<JsonElement>("/staff/lab-requests?status=pending", Token);
        var version = before.GetProperty("data").EnumerateArray().Single(item => item.GetProperty("id").GetString() == labId)
            .GetProperty("version").GetUInt32();

        await OkAsync(await AttachAsync(lab, labId, first));

        var stale = new HttpRequestMessage(HttpMethod.Post, $"/staff/lab-requests/{labId}/attachments")
        {
            Content = JsonContent.Create(new { file = second, file_name = "x.png" }),
        };
        stale.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        var response = await lab.SendAsync(stale, Token);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("version_conflict", await ErrorCodeAsync(response, Token));
    }
}
