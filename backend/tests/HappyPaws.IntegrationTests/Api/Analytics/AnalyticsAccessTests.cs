using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.AnalyticsScenario;

namespace HappyPaws.IntegrationTests.Api.Analytics;

/// <summary>
/// Who may read which report, about which branch, and over what span. The numbers themselves are
/// in the other classes of this folder.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AnalyticsAccessTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Every report and the one scope it needs.</summary>
    public static TheoryData<string, string> Reports => new()
    {
        { "/staff/dashboard/today", Scopes.Dashboard.Read },
        { "/staff/stats/appointments", Scopes.Stats.Read },
        { "/staff/stats/queue", Scopes.Stats.Read },
        { "/staff/stats/workload", Scopes.Stats.Read },
        { "/staff/analytics/revenue", Scopes.Analytics.Read },
        { "/staff/analytics/services", Scopes.Analytics.Read },
        { "/staff/analytics/retention", Scopes.Analytics.Read },
        { "/staff/analytics/staff-performance", Scopes.Analytics.Hr },
    };

    /// <summary>The reports that take a span of days.</summary>
    public static TheoryData<string> RangedReports => new()
    {
        "/staff/stats/appointments",
        "/staff/stats/queue",
        "/staff/stats/workload",
        "/staff/analytics/revenue",
        "/staff/analytics/services",
        "/staff/analytics/retention",
        "/staff/analytics/staff-performance",
    };

    private static async Task<JsonElement> ErrorOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    [Theory]
    [MemberData(nameof(Reports))]
    public async Task Each_report_refuses_a_caller_without_its_scope_and_names_it(string path, string scope)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        // Every other report scope, so the refusal can only be about this one. analytics:read in
        // particular must not open the HR report.
        using var reader = fixture.Reader(clinic, [.. ReportScopes.Where(other => other != scope), Scopes.Invoice.Write]);

        using var response = await reader.GetAsync(path, Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await ErrorOf(response);
        Assert.Equal("insufficient_scope", error.GetProperty("code").GetString());
        Assert.Contains(
            scope,
            error.GetProperty("details").GetProperty("missing_scopes").EnumerateArray().Select(item => item.GetString()));
    }

    [Theory]
    [MemberData(nameof(Reports))]
    public async Task Each_report_refuses_a_branch_the_caller_does_not_work_at(string path, string scope)
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.Reader(mine, scope);

        using var response = await reader.GetAsync($"{path}?branch={theirs.BranchId}", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("forbidden", (await ErrorOf(response)).GetProperty("code").GetString());
    }

    [Theory]
    [MemberData(nameof(RangedReports))]
    public async Task A_span_that_ends_before_it_starts_is_refused(string path)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.Reader(clinic);

        using var response = await reader.GetAsync($"{path}?from=2026-09-10&to=2026-09-01", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await ErrorOf(response);
        Assert.Equal("validation_failed", error.GetProperty("code").GetString());
        Assert.Equal("to", error.GetProperty("field").GetString());
    }

    [Theory]
    [MemberData(nameof(RangedReports))]
    public async Task A_span_longer_than_a_year_and_a_day_is_refused(string path)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.Reader(clinic);

        using var response = await reader.GetAsync($"{path}?from=2025-01-01&to=2026-06-30", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("to", (await ErrorOf(response)).GetProperty("field").GetString());
    }

    [Theory]
    [InlineData("/staff/stats/appointments?from=yesterday")]
    [InlineData("/staff/analytics/revenue?to=2026-13-01")]
    [InlineData("/staff/stats/appointments?granularity=hour")]
    [InlineData("/staff/analytics/revenue?granularity=year")]
    public async Task A_malformed_date_or_granularity_is_refused(string path)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.Reader(clinic);

        using var response = await reader.GetAsync(path, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("validation_failed", (await ErrorOf(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Without_a_span_the_reports_cover_the_last_thirty_days_and_retention_six_months()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.Reader(clinic);

        var stats = await ReadAsync(reader, $"/staff/stats/appointments?branch={clinic.BranchId}", Token);
        var retention = await ReadAsync(reader, $"/staff/analytics/retention?branch={clinic.BranchId}", Token);

        Assert.Equal(Today.ToString("yyyy-MM-dd"), stats.GetProperty("to").GetString());
        Assert.Equal(Today.AddDays(-29).ToString("yyyy-MM-dd"), stats.GetProperty("from").GetString());
        Assert.Equal(30, stats.GetProperty("series").GetArrayLength());
        Assert.Equal(6, retention.GetProperty("months").GetArrayLength());
    }
}
