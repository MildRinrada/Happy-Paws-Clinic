using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Customers;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api.Crm;

/// <summary>
/// <c>POST /staff/integrations/line/broadcast</c> with a <c>segment_id</c>: the group is worked out at
/// send time and narrowed to members who agreed to marketing and have LINE.
/// </summary>
[Collection(nameof(DeliveryCollection))]
public class SegmentBroadcastTests(DeliveryFixture fixture) : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        fixture.Transports.Reset();
        fixture.Line.Status = HttpStatusCode.OK;
        await NotificationScenario.ConfigureLineAsync(fixture, cancellationToken: Token);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task A_segment_broadcast_reaches_its_members_who_agreed_and_no_one_else()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (other, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var desk = fixture.UnboundCrmDesk(clinic);

        await BillingScenario.RaiseAsync(desk, clinic, Token);
        await BillingScenario.RaiseAsync(desk, clinic, Token, new
        {
            customer_id = other,
            pet_id = otherPet,
            branch_id = clinic.BranchId,
            lines = new object[] { new { description = "ตรวจ", quantity = 1, unit_price_satang = 10_000 } },
        });

        var agreedLine = await NotificationScenario.GiveLineAddressAsync(fixture, clinic.CustomerId, Token);
        await NotificationScenario.ConsentAsync(fixture, clinic.CustomerId, ConsentPurpose.Marketing, true, Token);
        var notAskedLine = await NotificationScenario.GiveLineAddressAsync(fixture, other, Token);

        var created = await desk.PostAsJsonAsync(
            "/staff/segments", new { name = "ลูกค้าสาขานี้", criteria = new { branch_id = clinic.BranchId } }, Token);
        created.EnsureSuccessStatusCode();
        var segmentId = (await created.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString();

        using var broadcaster = fixture.Notifier(Scopes.Notify.Broadcast);
        var response = await broadcaster.PostAsJsonAsync(
            "/staff/integrations/line/broadcast", new { segment_id = segmentId, message = "โปรวัคซีนเดือนนี้" }, Token);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal(1, body.GetProperty("sent").GetInt32());
        Assert.Single(fixture.Line.PushesTo(agreedLine));
        Assert.Empty(fixture.Line.PushesTo(notAskedLine));
    }

    [Fact]
    public async Task A_broadcast_names_customers_or_a_segment_not_both_and_the_segment_must_exist()
    {
        var customer = await NotificationScenario.CustomerAsync(fixture, cancellationToken: Token);
        using var broadcaster = fixture.Notifier(Scopes.Notify.Broadcast);

        var both = await broadcaster.PostAsJsonAsync(
            "/staff/integrations/line/broadcast",
            new { customer_ids = new[] { customer.CustomerId }, segment_id = "seg_x", message = "ทดสอบ" },
            Token);
        var missing = await broadcaster.PostAsJsonAsync(
            "/staff/integrations/line/broadcast", new { segment_id = "seg_does_not_exist", message = "ทดสอบ" }, Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, both.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
