using System.Net;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Patients;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.InventoryScenario;

namespace HappyPaws.IntegrationTests.Api.Forecasting;

/// <summary>
/// <c>GET /staff/customers/due-back</c> (<c>docs/contract-extensions.md § 33</c>): owners of pets
/// with a vaccine overdue, and customers later back than their own usual gap, with nothing booked.
/// </summary>
[Collection(nameof(ApiCollection))]
public class DueBackTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Desk(SeededClinic clinic, params string[] scopes) =>
        fixture.CrmDesk(clinic, scopes.Length == 0 ? [Scopes.Crm.Read] : scopes);

    [Fact]
    public async Task An_overdue_latest_dose_with_nothing_booked_puts_the_owner_on_the_list()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var today = ForecastScenario.Today;
        await ForecastScenario.VisitsAsync(fixture, clinic, clinic.CustomerId, clinic.PetId, [300], Token);

        await AutomationScenario.VaccinationAsync(fixture, clinic.PetId, "Rabies", today.AddDays(-20), cancellationToken: Token);
        await AutomationScenario.VaccinationAsync(fixture, clinic.PetId, "DHPP", today.AddDays(30), cancellationToken: Token);

        // A newer dose of the same vaccine (written differently) replaces an overdue one.
        await AutomationScenario.VaccinationAsync(fixture, clinic.PetId, "FeLV", today.AddDays(-50), givenDaysAgo: 400, cancellationToken: Token);
        await AutomationScenario.VaccinationAsync(fixture, clinic.PetId, "felv", today.AddDays(200), givenDaysAgo: 10, cancellationToken: Token);

        // Another owner at the branch: overdue, but a visit is booked.
        var booked = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        await ForecastScenario.VisitsAsync(fixture, clinic, booked.CustomerId, booked.PetId, [200], Token);
        await AutomationScenario.VaccinationAsync(fixture, booked.PetId, "Rabies", today.AddDays(-5), cancellationToken: Token);
        await ForecastScenario.AppointmentAsync(
            fixture, clinic, DateTimeOffset.UtcNow.AddDays(3), customerId: booked.CustomerId, petId: booked.PetId, cancellationToken: Token);

        // And one whose pet has died.
        var deceased = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        await ForecastScenario.VisitsAsync(fixture, clinic, deceased.CustomerId, deceased.PetId, [200], Token);
        await AutomationScenario.VaccinationAsync(fixture, deceased.PetId, "Rabies", today.AddDays(-5), cancellationToken: Token);
        await MarkDeceasedAsync(deceased.PetId);

        using var desk = Desk(clinic);
        var page = await OkAsync(await desk.GetAsync("/staff/customers/due-back?reason=vaccine_overdue", Token), Token);

        var row = Assert.Single(page.GetProperty("data").EnumerateArray());
        Assert.Equal(clinic.CustomerId, row.GetProperty("customer_id").GetString());
        Assert.Equal("vaccine_overdue", row.GetProperty("reason").GetString());
        Assert.Equal(today.AddDays(-20).ToString("yyyy-MM-dd"), row.GetProperty("due_on").GetString());
        Assert.Equal(20, row.GetProperty("days_overdue").GetInt32());
        Assert.Equal(today.AddDays(-300).ToString("yyyy-MM-dd"), row.GetProperty("last_visit_on").GetString());
        Assert.False(row.TryGetProperty("usual_interval_days", out _));

        var pet = Assert.Single(row.GetProperty("pets").EnumerateArray());
        Assert.Equal(clinic.PetId, pet.GetProperty("pet_id").GetString());
        var dose = Assert.Single(pet.GetProperty("overdue_vaccines").EnumerateArray());
        Assert.Equal("Rabies", dose.GetProperty("vaccine_name").GetString());
    }

    [Fact]
    public async Task A_customer_later_than_their_own_rhythm_is_lapsed_and_others_are_not()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        // Monthly: visits 130, 100 and 70 days ago (a bill on the same day counts once). Median gap
        // 30, allowed 60, due back 10 days ago.
        await ForecastScenario.VisitsAsync(fixture, clinic, clinic.CustomerId, clinic.PetId, [130, 100, 70], Token, billSameDay: true);

        // The same rhythm, but a visit is booked.
        var booked = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        await ForecastScenario.VisitsAsync(fixture, clinic, booked.CustomerId, booked.PetId, [130, 100, 70], Token);
        await ForecastScenario.AppointmentAsync(
            fixture, clinic, DateTimeOffset.UtcNow.AddDays(5), customerId: booked.CustomerId, petId: booked.PetId, cancellationToken: Token);

        // Two visits only: no rhythm to judge.
        var twice = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        await ForecastScenario.VisitsAsync(fixture, clinic, twice.CustomerId, twice.PetId, [400, 300], Token);

        // Once a year: 400 days without a visit is not late for them yet.
        var yearly = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        await ForecastScenario.VisitsAsync(fixture, clinic, yearly.CustomerId, yearly.PetId, [1130, 765, 400], Token);

        using var desk = Desk(clinic);
        var page = await OkAsync(await desk.GetAsync($"/staff/customers/due-back?reason=lapsed&branch={clinic.BranchId}", Token), Token);

        var row = Assert.Single(page.GetProperty("data").EnumerateArray());
        Assert.Equal(clinic.CustomerId, row.GetProperty("customer_id").GetString());
        Assert.Equal("lapsed", row.GetProperty("reason").GetString());
        Assert.Equal(3, row.GetProperty("visit_count").GetInt32());
        Assert.Equal(30, row.GetProperty("usual_interval_days").GetDouble());
        Assert.Equal(60, row.GetProperty("allowed_days").GetInt32());
        Assert.Equal(ForecastScenario.Today.AddDays(-70).ToString("yyyy-MM-dd"), row.GetProperty("last_visit_on").GetString());
        Assert.Equal(10, row.GetProperty("days_overdue").GetInt32());
        Assert.Equal(clinic.PetId, Assert.Single(row.GetProperty("pets").EnumerateArray()).GetProperty("pet_id").GetString());
    }

    [Fact]
    public async Task The_list_pages_most_recently_due_first()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var older = await ClinicSeed.OtherOwnerAsync(fixture, Token);

        await ForecastScenario.VisitsAsync(fixture, clinic, clinic.CustomerId, clinic.PetId, [130, 100, 70], Token);
        await ForecastScenario.VisitsAsync(fixture, clinic, older.CustomerId, older.PetId, [140, 110, 80], Token);

        using var desk = Desk(clinic);
        var first = await OkAsync(await desk.GetAsync($"/staff/customers/due-back?reason=lapsed&branch={clinic.BranchId}&limit=1", Token), Token);

        Assert.Equal(clinic.CustomerId, Assert.Single(first.GetProperty("data").EnumerateArray()).GetProperty("customer_id").GetString());
        var cursor = first.GetProperty("page").GetProperty("next_cursor").GetString();
        Assert.False(string.IsNullOrEmpty(cursor));

        var second = await OkAsync(
            await desk.GetAsync($"/staff/customers/due-back?reason=lapsed&branch={clinic.BranchId}&limit=1&cursor={cursor}", Token), Token);

        Assert.Equal(older.CustomerId, Assert.Single(second.GetProperty("data").EnumerateArray()).GetProperty("customer_id").GetString());
        Assert.Equal(20, second.GetProperty("data")[0].GetProperty("days_overdue").GetInt32());
        Assert.False(second.GetProperty("page").TryGetProperty("next_cursor", out var next) && next.ValueKind == JsonValueKind.String);
    }

    [Fact]
    public async Task Another_branchs_customers_are_not_listed_and_not_readable()
    {
        var mine = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var theirs = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await ForecastScenario.VisitsAsync(fixture, theirs, theirs.CustomerId, theirs.PetId, [130, 100, 70], Token);

        using var desk = Desk(mine);
        var page = await OkAsync(await desk.GetAsync("/staff/customers/due-back?reason=lapsed", Token), Token);

        Assert.DoesNotContain(
            page.GetProperty("data").EnumerateArray(),
            row => row.GetProperty("customer_id").GetString() == theirs.CustomerId);
        Assert.Equal(HttpStatusCode.Forbidden, (await desk.GetAsync($"/staff/customers/due-back?reason=lapsed&branch={theirs.BranchId}", Token)).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("reason=inactive")]
    [InlineData("reason=lapsed&cursor=not-a-cursor")]
    public async Task A_missing_or_unknown_reason_or_a_broken_cursor_is_refused(string query)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = Desk(clinic);

        var response = await desk.GetAsync($"/staff/customers/due-back?{query}", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("validation_failed", await ErrorCodeAsync(response, Token));
    }

    [Fact]
    public async Task It_needs_a_session_and_crm_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/staff/customers/due-back?reason=lapsed", Token)).StatusCode);

        using var counter = Desk(clinic, Scopes.Customer.Read, Scopes.Crm.Manage);
        var refused = await counter.GetAsync("/staff/customers/due-back?reason=lapsed", Token);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("insufficient_scope", await ErrorCodeAsync(refused, Token));
    }

    private async Task MarkDeceasedAsync(string petId)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var pet = await db.Set<Pet>().SingleAsync(item => item.Id == petId, Token);

        pet.MarkDeceased(DateTimeOffset.UtcNow.AddDays(-1));
        await db.SaveChangesAsync(Token);
    }
}
