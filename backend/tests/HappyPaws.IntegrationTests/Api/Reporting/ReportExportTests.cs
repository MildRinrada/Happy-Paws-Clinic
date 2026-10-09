using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HappyPaws.Application.Reporting.RunExports;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Reporting;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.AutomationScenario;

namespace HappyPaws.IntegrationTests.Api.Reporting;

/// <summary>
/// Export jobs: who may ask, what is refused, the branch the file is cut to, the worker that builds
/// it, and the queue two workers share without taking the same job.
/// </summary>
[Collection(nameof(JobsCollection))]
public class ReportExportTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string[] Exporter = [Scopes.Report.Export, Scopes.Stats.Read, Scopes.Analytics.Read, Scopes.Analytics.Hr];

    private HttpClient ExporterOf(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, scopes.Length == 0 ? Exporter : scopes, [clinic.BranchId]));

    [Fact]
    public async Task Both_endpoints_need_report_export_and_a_signed_in_caller()
    {
        using var without = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Stats.Read, Scopes.Analytics.Read]));
        using var anonymous = fixture.CreateClient();

        var post = await without.PostAsJsonAsync("/staff/reports/exports", new { report = "appointments" }, Token);
        var get = await without.GetAsync("/staff/reports/exports/rex_x", Token);

        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/staff/reports/exports", new { report = "appointments" }, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/reports/exports/rex_x", Token)).StatusCode);
    }

    [Theory]
    [InlineData("staff_performance", Scopes.Analytics.Hr)]
    [InlineData("revenue", Scopes.Analytics.Read)]
    [InlineData("queue", Scopes.Stats.Read)]
    public async Task An_export_also_needs_the_scope_of_the_report_it_copies(string report, string missing)
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [.. Exporter.Where(scope => scope != missing)]));

        var response = await client.PostAsJsonAsync("/staff/reports/exports", new { report }, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.Equal("insufficient_scope", error.GetProperty("code").GetString());
        Assert.Contains(missing, error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(scope => scope.GetString()));
    }

    public static TheoryData<string, string> BadRequests => new()
    {
        { "report", """{"report":"everything"}""" },
        { "report", """{}""" },
        { "format", """{"report":"revenue","format":"docx"}""" },
        { "to", """{"report":"revenue","from":"2026-10-10","to":"2026-10-01"}""" },
        { "to", """{"report":"revenue","from":"2025-01-01","to":"2026-10-01"}""" },
    };

    [Theory]
    [MemberData(nameof(BadRequests))]
    public async Task An_export_that_cannot_be_built_is_refused_with_the_field_named(string field, string json)
    {
        using var client = fixture.Authenticated(TestTokens.Staff(fixture, scopes: Exporter));

        var response = await client.PostAsync("/staff/reports/exports", new StringContent(json, Encoding.UTF8, "application/json"), Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");
        Assert.True(error.GetProperty("details").GetProperty("errors").TryGetProperty(field, out _), $"no error on {field}: {error}");
    }

    [Fact]
    public async Task A_branch_bound_export_is_cut_to_the_requesters_branches_and_refuses_another()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = ExporterOf(clinic);

        var refused = await client.PostAsJsonAsync("/staff/reports/exports", new { report = "appointments", branch_id = other.BranchId }, Token);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);

        var accepted = await client.PostAsJsonAsync("/staff/reports/exports", new { report = "appointments" }, Token);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var job = await accepted.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(new[] { clinic.BranchId }, job.GetProperty("branch_ids").EnumerateArray().Select(branch => branch.GetString()));
    }

    [Fact]
    public async Task An_export_is_queued_built_by_the_worker_and_downloaded_once_finished()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var day = ClinicToday.AddDays(2);
        await AppointmentAsync(fixture, clinic, ClinicTime(day, 10), cancellationToken: Token);
        await AppointmentAsync(fixture, clinic, ClinicTime(day, 11), appointment => appointment.Cancel(DateTimeOffset.UtcNow, "usr_test", "ติดธุระ"), cancellationToken: Token);
        using var client = ExporterOf(clinic);

        var response = await client.PostAsJsonAsync(
            "/staff/reports/exports",
            new { report = "appointments", format = "csv", from = day.ToString("yyyy-MM-dd"), to = day.ToString("yyyy-MM-dd"), branch_id = clinic.BranchId },
            Token);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var queued = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        var id = queued.GetProperty("id").GetString()!;
        Assert.StartsWith("rex_", id);
        Assert.Equal("queued", queued.GetProperty("status").GetString());
        Assert.EndsWith($"/staff/reports/exports/{id}", response.Headers.Location!.ToString());
        Assert.False(queued.TryGetProperty("download_url", out _));

        await RunExportsAsync();

        var done = await client.GetFromJsonAsync<JsonElement>($"/staff/reports/exports/{id}", Token);
        Assert.Equal("succeeded", done.GetProperty("status").GetString());
        Assert.Equal(2, done.GetProperty("row_count").GetInt32());
        Assert.Equal($"appointments_{day:yyyy-MM-dd}_{day:yyyy-MM-dd}.csv", done.GetProperty("file_name").GetString());

        var expiresAt = done.GetProperty("download_expires_at").GetDateTimeOffset();
        Assert.InRange(expiresAt - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(55), TimeSpan.FromMinutes(61));

        // The link is the storage's own signed URL; no session needed, as with S3.
        using var anonymous = fixture.CreateClient();
        var download = await anonymous.GetAsync(new Uri(done.GetProperty("download_url").GetString()!).PathAndQuery, Token);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("text/csv", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal($"appointments_{day:yyyy-MM-dd}_{day:yyyy-MM-dd}.csv", download.Content.Headers.ContentDisposition!.FileName?.Trim('"'));

        var bytes = await download.Content.ReadAsByteArrayAsync(Token);
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("วันที่,สถานะ", lines[0]);
        Assert.Contains(lines, line => line.StartsWith($"{day:yyyy-MM-dd},booked,", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith($"{day:yyyy-MM-dd},cancelled,", StringComparison.Ordinal));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.True(await db.Set<AuditEntry>().AsNoTracking()
            .AnyAsync(entry => entry.Action == AuditActions.ReportExported && entry.ResourceId == id, Token));
    }

    [Theory]
    [InlineData("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("pdf", "application/pdf")]
    public async Task An_export_can_be_a_workbook_or_a_printable_pdf_through_the_same_queue(string format, string mediaType)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var day = ClinicToday.AddDays(3);
        await AppointmentAsync(fixture, clinic, ClinicTime(day, 10), cancellationToken: Token);
        await AppointmentAsync(fixture, clinic, ClinicTime(day, 11), appointment => appointment.Cancel(DateTimeOffset.UtcNow, "usr_test", "ติดธุระ"), cancellationToken: Token);
        using var client = ExporterOf(clinic);

        var response = await client.PostAsJsonAsync(
            "/staff/reports/exports",
            new { report = "appointments", format, from = day.ToString("yyyy-MM-dd"), to = day.ToString("yyyy-MM-dd"), branch_id = clinic.BranchId },
            Token);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var queued = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(format, queued.GetProperty("format").GetString());
        var id = queued.GetProperty("id").GetString()!;

        await RunExportsAsync();

        var done = await client.GetFromJsonAsync<JsonElement>($"/staff/reports/exports/{id}", Token);
        var fileName = $"appointments_{day:yyyy-MM-dd}_{day:yyyy-MM-dd}.{format}";
        Assert.Equal("succeeded", done.GetProperty("status").GetString());
        Assert.Equal(format, done.GetProperty("format").GetString());
        Assert.Equal(2, done.GetProperty("row_count").GetInt32());
        Assert.Equal(fileName, done.GetProperty("file_name").GetString());

        using var anonymous = fixture.CreateClient();
        var download = await anonymous.GetAsync(new Uri(done.GetProperty("download_url").GetString()!).PathAndQuery, Token);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(mediaType, download.Content.Headers.ContentType!.MediaType);
        Assert.Equal(fileName, download.Content.Headers.ContentDisposition!.FileName?.Trim('"'));

        var bytes = await download.Content.ReadAsByteArrayAsync(Token);
        Assert.Equal(bytes.LongLength, done.GetProperty("size_bytes").GetInt64());

        if (format == "pdf")
        {
            Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes[..5]));
            return;
        }

        using var workbook = SpreadsheetDocument.Open(new MemoryStream(bytes), isEditable: false);
        var sheet = workbook.WorkbookPart!.WorksheetParts.Single().Worksheet!;
        var rows = sheet.Descendants<Row>().ToList();
        Assert.Equal(3, rows.Count);

        var header = rows[0].Elements<Cell>().Select(cell => cell.InlineString!.Text!.Text).ToList();
        Assert.Equal(["วันที่", "สถานะ", "walk-in", "ยกเลิกกระชั้นชิด", "จองออนไลน์", "จำนวนนัด"], header);

        var statuses = rows.Skip(1).Select(row => row.Elements<Cell>().ElementAt(1).InlineString!.Text!.Text).Order().ToList();
        Assert.Equal(["booked", "cancelled"], statuses);

        foreach (var row in rows.Skip(1))
        {
            var cells = row.Elements<Cell>().ToList();
            Assert.Equal(day.ToDateTime(TimeOnly.MinValue), DateTime.FromOADate(double.Parse(cells[0].CellValue!.Text, CultureInfo.InvariantCulture)));
            Assert.Equal(CellValues.Boolean, cells[2].DataType?.Value);
            Assert.Equal(CellValues.Number, cells[5].DataType?.Value);
            Assert.Equal("1", cells[5].CellValue!.Text);
        }

        Assert.Empty(sheet.Descendants<CellFormula>());
    }

    [Fact]
    public async Task Only_the_member_who_asked_can_read_an_export()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = ExporterOf(clinic);
        var id = (await (await client.PostAsJsonAsync("/staff/reports/exports", new { report = "workload" }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!;

        using var colleague = fixture.Authenticated(TestTokens.Staff(fixture, "usr_other", "stf_other", Exporter, [clinic.BranchId]));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/staff/reports/exports/{id}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await colleague.GetAsync($"/staff/reports/exports/{id}", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/staff/reports/exports/rex_nope", Token)).StatusCode);
    }

    [Fact]
    public async Task Workers_claiming_at_once_never_take_the_same_job()
    {
        await RunExportsAsync();

        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = ExporterOf(clinic);
        var ids = new List<string>();

        for (var i = 0; i < 2; i++)
        {
            var job = await (await client.PostAsJsonAsync("/staff/reports/exports", new { report = "queue" }, Token))
                .Content.ReadFromJsonAsync<JsonElement>(Token);
            ids.Add(job.GetProperty("id").GetString()!);
        }

        // Four workers, each with its own scope and connection, as four API instances would be.
        var claims = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            using var scope = fixture.CreateScope();
            return (await scope.ServiceProvider.GetRequiredService<RunReportExportsHandler>().ClaimAsync(Token))?.Id;
        }, Token)));

        var taken = claims.Where(claim => claim is not null).ToList();
        Assert.Equal(ids.Order(), taken.Order());
        Assert.Equal(2, claims.Count(claim => claim is null));
    }

    [Fact]
    public async Task A_job_left_running_by_a_stopped_worker_is_retried_and_finished()
    {
        await RunExportsAsync();

        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var client = ExporterOf(clinic);
        var id = (await (await client.PostAsJsonAsync("/staff/reports/exports", new { report = "revenue" }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!;

        using (var scope = fixture.CreateScope())
        {
            Assert.Equal(id, (await scope.ServiceProvider.GetRequiredService<RunReportExportsHandler>().ClaimAsync(Token))?.Id);

            // The worker "dies": its job stays running, started long ago.
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE report_exports SET started_at = {DateTimeOffset.UtcNow.AddMinutes(-30)} WHERE id = {id}", Token);
        }

        await RunExportsAsync();

        using var check = fixture.CreateScope();
        var export = await check.ServiceProvider.GetRequiredService<HappyPawsDbContext>().Set<ReportExport>().AsNoTracking()
            .FirstAsync(item => item.Id == id, Token);
        Assert.Equal(ReportExportStatus.Succeeded, export.Status);
        Assert.Equal(2, export.Attempts);
    }

    /// <summary>The worker's run, until the queue is empty.</summary>
    private async Task RunExportsAsync()
    {
        for (var round = 0; round < 20; round++)
        {
            using var scope = fixture.CreateScope();

            if (await scope.ServiceProvider.GetRequiredService<RunReportExportsHandler>().RunAsync(Token) == 0)
            {
                return;
            }
        }
    }
}
