using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.BillingScenario;
using static HappyPaws.IntegrationTests.Fixtures.CrmScenario;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary><c>docs/contract-extensions.md § 25</c>: <c>POST /staff/loyalty/{customer_id}/adjustments</c>.</summary>
[Collection(nameof(ApiCollection))]
public class PointsAdjustmentTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<HttpResponseMessage> AdjustAsync(HttpClient client, string customerId, object body, string? key = null)
    {
        using var request = BookingScenario.Idempotent($"/staff/loyalty/{customerId}/adjustments", body, key);
        return await client.SendAsync(request, Token);
    }

    [Fact]
    public async Task Adjusting_needs_crm_manage_and_an_idempotency_key()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.CrmDesk(clinic, Scopes.Crm.Read);
        using var desk = fixture.CrmDesk(clinic);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await AdjustAsync(reader, clinic.CustomerId, new { points = 10, reason = "ชดเชย" })).StatusCode);

        var unkeyed = await desk.PostAsJsonAsync($"/staff/loyalty/{clinic.CustomerId}/adjustments", new { points = 10, reason = "ชดเชย" }, Token);
        Assert.Equal("idempotency_key_required", await ErrorCodeAsync(unkeyed, Token));
    }

    [Theory]
    [InlineData(0, "ชดเชย", "points")]
    [InlineData(10, " ", "reason")]
    [InlineData(100_001, "ชดเชย", "points")]
    public async Task Points_must_be_non_zero_and_bounded_and_a_reason_is_required(int points, string reason, string field)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);

        var response = await AdjustAsync(desk, clinic.CustomerId, new { points, reason });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(field, body.GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task Points_given_by_hand_move_the_balance_not_the_level_and_are_audited()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var key = Guid.NewGuid().ToString();

        var response = await AdjustAsync(desk, clinic.CustomerId, new { points = 500, reason = "ชดเชยนัดที่คลินิกเลื่อน" }, key);
        var retried = await AdjustAsync(desk, clinic.CustomerId, new { points = 500, reason = "ชดเชยนัดที่คลินิกเลื่อน" }, key);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retried.StatusCode);
        var adjusted = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(500, adjusted.GetProperty("points_balance").GetInt32());
        Assert.Equal("adjust", adjusted.GetProperty("entry").GetProperty("kind").GetString());
        Assert.Equal(clinic.StaffId, adjusted.GetProperty("entry").GetProperty("staff_id").GetString());

        var account = await LoyaltyAsync(desk, clinic.CustomerId, Token);
        Assert.Equal(500, account.GetProperty("points_balance").GetInt32());
        Assert.Equal(0, account.GetProperty("lifetime_points").GetInt32());
        Assert.Equal("member", account.GetProperty("tier").GetString());
        var row = Assert.Single(account.GetProperty("history").EnumerateArray());
        Assert.Equal("ชดเชยนัดที่คลินิกเลื่อน", row.GetProperty("note").GetString());

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        Assert.Equal(
            1,
            await db.Set<AuditEntry>().CountAsync(entry => entry.Action == AuditActions.LoyaltyAdjusted && entry.ResourceId == clinic.CustomerId, Token));
    }

    [Fact]
    public async Task Taking_points_cannot_take_the_balance_below_zero()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);
        var invoiceId = await RaiseIdAsync(desk, clinic, Token);
        (await PayAsync(desk, invoiceId, 146_000, Token)).EnsureSuccessStatusCode();

        var tooMany = await AdjustAsync(desk, clinic.CustomerId, new { points = -15, reason = "ให้แต้มผิดบิล" });
        Assert.Equal(HttpStatusCode.Conflict, tooMany.StatusCode);
        Assert.Equal("insufficient_points", await ErrorCodeAsync(tooMany, Token));

        var fits = await AdjustAsync(desk, clinic.CustomerId, new { points = -14, reason = "ให้แต้มผิดบิล" });
        Assert.Equal(HttpStatusCode.Created, fits.StatusCode);

        var account = await LoyaltyAsync(desk, clinic.CustomerId, Token);
        Assert.Equal(0, account.GetProperty("points_balance").GetInt32());
        Assert.Equal(14, account.GetProperty("lifetime_points").GetInt32());
    }

    [Fact]
    public async Task An_unknown_customer_is_not_found()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var desk = fixture.CrmDesk(clinic);

        var response = await AdjustAsync(desk, "cus_does_not_exist", new { points = 5, reason = "ชดเชย" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
