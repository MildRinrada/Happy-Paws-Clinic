using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;

namespace HappyPaws.IntegrationTests.Api;

/// <summary>
/// One list envelope, everywhere: <c>{ "data": [...], "page": { "next_cursor": "..." } }</c>.
/// <para>
/// The contract states it once for every paginated endpoint ("คืน data[] และ page.next_cursor"). A
/// client pages by reading <c>page.next_cursor</c>, so a list that put the cursor anywhere else
/// would look to that client like a list with exactly one page.
/// </para>
/// </summary>
[Collection(nameof(ApiCollection))]
public class PaginationEnvelopeTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/staff/customers", Scopes.Customer.Read)]
    [InlineData("/staff/pets", Scopes.Patient.Read)]
    [InlineData("/staff/invoices", Scopes.Invoice.Write)]
    [InlineData("/staff/audit-logs", Scopes.Audit.Read)]
    [InlineData("/staff/members", Scopes.Staff.Read)]
    [InlineData("/staff/appointments", Scopes.Appointment.Read)]
    public async Task Every_paginated_list_carries_its_cursor_under_page(string path, string scope)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var staff = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [scope], [clinic.BranchId]));

        var list = await staff.GetFromJsonAsync<JsonElement>($"{path}?limit=1", Token);

        Assert.Equal(JsonValueKind.Array, list.GetProperty("data").ValueKind);
        Assert.Equal(JsonValueKind.Object, list.GetProperty("page").ValueKind);

        // Never at the top level, where a client written against the contract would not look.
        Assert.False(list.TryGetProperty("next_cursor", out _));
    }

    [Fact]
    public async Task The_cursor_a_list_hands_out_fetches_the_next_page()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using var counter = fixture.Authenticated(TestTokens.Staff(
            fixture, clinic.StaffUserId, clinic.StaffId, [Scopes.Customer.Read], [clinic.BranchId]));

        // The suite shares a database that many tests have added customers to, so there is
        // always more than one page of one.
        var first = await counter.GetFromJsonAsync<JsonElement>("/staff/customers?limit=1", Token);
        var cursor = first.GetProperty("page").GetProperty("next_cursor").GetString();

        var second = await counter.GetFromJsonAsync<JsonElement>(
            $"/staff/customers?limit=1&cursor={cursor}", Token);

        Assert.NotEqual(
            first.GetProperty("data")[0].GetProperty("id").GetString(),
            second.GetProperty("data")[0].GetProperty("id").GetString());
    }
}
