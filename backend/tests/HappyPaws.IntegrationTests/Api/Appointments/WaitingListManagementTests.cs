using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Scheduling;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Auditing;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Appointments;

/// <summary>
/// <c>GET</c> and <c>DELETE /staff/waiting-list</c> (<c>docs/contract-extensions.md § 23</c>): the
/// list in the order people are told, its filters, the branch rule, and removing an entry.
/// </summary>
[Collection(nameof(ApiCollection))]
public class WaitingListManagementTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<string> AddAsync(HttpClient staff, string petId, string serviceId, string? branchId = null)
    {
        var response = await staff.PostAsJsonAsync(
            "/staff/waiting-list", new { pet_id = petId, service_id = serviceId, branch_id = branchId, note = "ช่วงเช้า" }, Token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("id").GetString()!;
    }

    private static async Task<List<JsonElement>> ListAsync(HttpClient staff, string query)
    {
        var page = await staff.GetFromJsonAsync<JsonElement>($"/staff/waiting-list?{query}", Token);
        return [.. page.GetProperty("data").EnumerateArray()];
    }

    private static List<string?> Ids(IEnumerable<JsonElement> rows) => [.. rows.Select(row => row.GetProperty("id").GetString())];

    /// <summary>An entry whose last day has passed, which no endpoint can make.</summary>
    private async Task<string> LapsedEntryAsync(SeededClinic clinic, string petId, string customerId)
    {
        using var scope = fixture.CreateScope();
        var today = scope.ServiceProvider.GetRequiredService<ClinicDays>().Today;
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        var entry = WaitingListEntry.Add(
            customerId, petId, clinic.ServiceId, today.AddDays(-10), today.AddDays(-3), today.AddDays(-10), clinic.StaffId, clinic.BranchId);

        db.Set<WaitingListEntry>().Add(entry);
        await db.SaveChangesAsync(Token);

        return entry.Id;
    }

    [Fact]
    public async Task Listing_and_removing_need_appointment_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.StaffOf(clinic, Scopes.Appointment.Read);

        var list = await reader.GetAsync("/staff/waiting-list", Token);
        var remove = await reader.DeleteAsync("/staff/waiting-list/wtl_anything", Token);

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, remove.StatusCode);
    }

    [Fact]
    public async Task The_list_shows_who_is_waiting_oldest_first_with_names_and_filters()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (otherCustomer, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var (thirdCustomer, thirdPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        var elsewhere = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        var first = await AddAsync(staff, clinic.PetId, clinic.ServiceId, clinic.BranchId);
        var second = await AddAsync(staff, otherPet, clinic.ServiceId);
        var otherService = await AddAsync(staff, otherPet, elsewhere.ServiceId);
        var lapsed = await LapsedEntryAsync(clinic, thirdPet, thirdCustomer);

        var waiting = await ListAsync(staff, $"service_id={clinic.ServiceId}&status=waiting");

        Assert.Equal([first, second], Ids(waiting));
        Assert.Equal("waiting", waiting[0].GetProperty("status").GetString());
        Assert.Equal("ลูน่า", waiting[0].GetProperty("pet_name").GetString());
        Assert.Equal("ตรวจสุขภาพ", waiting[0].GetProperty("service_name").GetString());
        Assert.Equal(clinic.BranchId, waiting[0].GetProperty("branch_id").GetString());
        Assert.Equal(otherCustomer, waiting[1].GetProperty("customer_id").GetString());
        // Null fields are left out of the body, so "any branch" is no branch_id at all.
        Assert.False(waiting[1].TryGetProperty("branch_id", out var anyBranch) && anyBranch.ValueKind != JsonValueKind.Null);

        var lapsedOnly = await ListAsync(staff, $"service_id={clinic.ServiceId}&status=lapsed");
        Assert.Equal([lapsed], Ids(lapsedOnly));
        Assert.Equal("lapsed", lapsedOnly[0].GetProperty("status").GetString());

        var both = await ListAsync(staff, $"service_id={clinic.ServiceId}");
        Assert.Equal(3, both.Count);
        Assert.DoesNotContain(otherService, Ids(both));

        var paged = await staff.GetFromJsonAsync<JsonElement>($"/staff/waiting-list?service_id={clinic.ServiceId}&limit=1", Token);
        var next = paged.GetProperty("page").GetProperty("next_cursor").GetString();
        var rest = await ListAsync(staff, $"service_id={clinic.ServiceId}&limit=5&cursor={next}");
        Assert.Equal(Ids(both)[0], paged.GetProperty("data")[0].GetProperty("id").GetString());
        Assert.Equal(Ids(both).Skip(1), Ids(rest));
    }

    [Fact]
    public async Task An_unknown_status_is_refused()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        var response = await staff.GetAsync("/staff/waiting-list?status=withdrawn", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            "status",
            (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error").GetProperty("field").GetString());
    }

    [Fact]
    public async Task Branch_bound_staff_see_and_remove_only_their_branches_and_the_entries_for_any()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var other = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (_, anyPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var here = fixture.StaffOf(clinic);
        using var there = fixture.StaffOf(other);

        var mine = await AddAsync(here, clinic.PetId, clinic.ServiceId, clinic.BranchId);
        var theirs = await AddAsync(there, other.PetId, clinic.ServiceId, other.BranchId);
        var forAny = await AddAsync(here, anyPet, clinic.ServiceId);

        Assert.Equal([mine, forAny], Ids(await ListAsync(here, $"service_id={clinic.ServiceId}")));
        Assert.Equal([theirs, forAny], Ids(await ListAsync(there, $"service_id={clinic.ServiceId}")));
        Assert.Equal([mine, forAny], Ids(await ListAsync(here, $"service_id={clinic.ServiceId}&branch={clinic.BranchId}")));

        var otherBranch = await here.GetAsync($"/staff/waiting-list?branch={other.BranchId}", Token);
        Assert.Equal(HttpStatusCode.Forbidden, otherBranch.StatusCode);

        var removeTheirs = await here.DeleteAsync($"/staff/waiting-list/{theirs}", Token);
        Assert.Equal(HttpStatusCode.Forbidden, removeTheirs.StatusCode);
        Assert.Contains(theirs, Ids(await ListAsync(there, $"service_id={clinic.ServiceId}")));

        // An entry for any branch is every branch's to remove.
        var removeAny = await there.DeleteAsync($"/staff/waiting-list/{forAny}", Token);
        Assert.Equal(HttpStatusCode.NoContent, removeAny.StatusCode);
    }

    [Fact]
    public async Task Removing_an_entry_deletes_it_audits_it_and_a_second_remove_is_not_found()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        var entry = await AddAsync(staff, clinic.PetId, clinic.ServiceId, clinic.BranchId);

        var removed = await staff.DeleteAsync($"/staff/waiting-list/{entry}", Token);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        Assert.Empty(await ListAsync(staff, $"service_id={clinic.ServiceId}"));

        var again = await staff.DeleteAsync($"/staff/waiting-list/{entry}", Token);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var audit = await db.Set<AuditEntry>().AsNoTracking()
            .Where(row => row.Action == AuditActions.WaitingListEntryRemoved && row.ResourceId == entry)
            .ToListAsync(Token);

        Assert.Single(audit);
        Assert.Equal(clinic.PetId, audit[0].PetId);

        // Removed means the pet may be put back on the list for the same service.
        await AddAsync(staff, clinic.PetId, clinic.ServiceId, clinic.BranchId);
    }
}
