using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Laboratory;

/// <summary>A fresh clinic with one lab request on it, for the tests of corrections, notes and files (§ 52 to § 54).</summary>
internal static class LabScenario
{
    /// <summary>Everything the lab module asks for: order, enter, read.</summary>
    public static readonly string[] LabScopes = [Scopes.Lab.Read, Scopes.Lab.Request, Scopes.Lab.Write];

    /// <summary>Orders one test at a fresh clinic; answers the clinic, a client holding <see cref="LabScopes"/>, and the request id.</summary>
    public static async Task<(SeededClinic Clinic, HttpClient Lab, string LabId)> RequestedAsync(
        ApiFixture fixture,
        CancellationToken token,
        string destination = "in_house")
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: token);
        var lab = fixture.Vet(clinic, LabScopes);

        var response = await lab.PostAsJsonAsync(
            "/staff/lab-requests",
            new
            {
                pet_id = clinic.PetId,
                branch_id = clinic.BranchId,
                destination,
                external_lab_name = destination == "external" ? "Vet Central Lab" : null,
                tests = new[] { new { name = "CBC", code = "CBC" } },
            },
            token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(token);

        return (clinic, lab, body.GetProperty("id").GetString()!);
    }

    /// <summary>Enters ALT 180 (high), HCT 22 (low) and Glucose 95 (normal); answers the request.</summary>
    public static async Task<JsonElement> ResultedAsync(HttpClient lab, string labId, CancellationToken token)
    {
        var response = await lab.PostAsJsonAsync(
            $"/staff/lab-requests/{labId}/results",
            new
            {
                results = new object[]
                {
                    new { analyte = "ALT", value = 180m, unit = "U/L", reference_low = 10m, reference_high = 100m },
                    new { analyte = "HCT", value = 22m, unit = "%", reference_low = 37m, reference_high = 55m },
                    new { analyte = "Glucose", value = 95m, unit = "mg/dL", reference_low = 70m, reference_high = 140m },
                },
            },
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(token);
    }

    /// <summary>The id of the value named <paramref name="analyte"/> on a request.</summary>
    public static string ResultId(JsonElement request, string analyte) =>
        request.GetProperty("results").EnumerateArray()
            .First(item => item.GetProperty("analyte").GetString() == analyte)
            .GetProperty("id").GetString()!;

    /// <summary>The value named <paramref name="analyte"/> on a request.</summary>
    public static JsonElement Result(JsonElement request, string analyte) =>
        request.GetProperty("results").EnumerateArray().First(item => item.GetProperty("analyte").GetString() == analyte);

    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response, CancellationToken token) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(token)).GetProperty("error").GetProperty("code").GetString();

    /// <summary>The audit rows of one action on one request, oldest first.</summary>
    public static async Task<List<AuditEntry>> AuditAsync(ApiFixture fixture, string action, string labId, CancellationToken token)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<AuditEntry>().AsNoTracking()
            .Where(entry => entry.Action == action && entry.ResourceId == labId)
            .OrderBy(entry => entry.OccurredAt)
            .ToListAsync(token);
    }

    /// <summary>A client of the same clinic's scopes that works only at another branch.</summary>
    public static HttpClient Elsewhere(ApiFixture fixture) =>
        fixture.Authenticated(TestTokens.Staff(fixture, scopes: LabScopes, branches: ["brn_elsewhere"]));
}
