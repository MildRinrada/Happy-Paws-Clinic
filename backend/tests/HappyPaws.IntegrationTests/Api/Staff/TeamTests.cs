using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Staff.SeedStaff;
using HappyPaws.Domain.Staff;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Api.Staff;

/// <summary>
/// Departments, public profiles and the public team page, a contract extension (§ 10).
/// <para>
/// On the separate instance, because <c>seed-staff</c> puts people on every open branch: in the
/// shared database that would be every other test's clinic too.
/// </para>
/// </summary>
[Collection(nameof(FreshInstanceCollection))]
public class TeamTests(ApiFixture fixture) : FreshInstanceTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    protected override ApiFixture Instance => fixture;

    private async Task<SeedStaffResult> SeedAsync()
    {
        using var scope = fixture.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SeedStaffHandler>().HandleAsync(Token);
    }

    private async Task<List<JsonElement>> TeamAsync(string query = "")
    {
        using var anonymous = fixture.CreateClient();

        return [.. (await anonymous.GetFromJsonAsync<JsonElement>($"/team{query}", Token))
            .GetProperty("data").EnumerateArray()];
    }

    private static IEnumerable<string?> Names(JsonElement department) =>
        department.GetProperty("members").EnumerateArray().Select(member => member.GetProperty("display_name").GetString());

    private static List<JsonElement> Data(JsonElement page) => [.. page.GetProperty("data").EnumerateArray()];

    private static string StaffIdOf(IEnumerable<JsonElement> members, string email) =>
        members.Single(member => member.GetProperty("email").GetString() == email).GetProperty("id").GetString()!;

    [Fact]
    public async Task Seeding_twice_adds_each_sample_person_once()
    {
        await SeedAsync();
        var second = await SeedAsync();

        Assert.Empty(second.Created);
        Assert.Equal(SampleStaff.All.Select(SampleStaff.EmailOf).Order(), second.Skipped.Order());
    }

    [Fact]
    public async Task Anyone_can_read_the_team_grouped_by_department_in_page_order()
    {
        await SeedAsync();

        var departments = await TeamAsync();
        var codes = departments.Select(department => department.GetProperty("department").GetString()).ToList();

        Assert.Equal(Departments.All.Select(department => department.Code), codes);
        Assert.Equal("ทีมสัตวแพทย์", departments[0].GetProperty("name").GetString());

        // Most experienced first: the clinic director started in 2006.
        var vets = Names(departments[0]).ToList();
        Assert.Equal("น.สพ. กิตติ วรกุล", vets[0]);
        Assert.Contains("สพ.ญ. มินท์ ศรีสุข", vets);
    }

    [Fact]
    public async Task The_public_sees_a_profile_and_nothing_that_belongs_inside_the_clinic()
    {
        await SeedAsync();

        var member = (await TeamAsync("?department=veterinary"))[0].GetProperty("members")[0];

        Assert.Equal("ผู้อำนวยการคลินิก", member.GetProperty("job_title").GetString());
        Assert.True(member.GetProperty("years_of_experience").GetInt32() > 0);

        string[] internalOnly = ["email", "roles", "branches", "license_no", "user_id", "status", "staff_type", "two_factor_enabled"];
        Assert.All(internalOnly, field => Assert.False(member.TryGetProperty(field, out _), field));
    }

    [Fact]
    public async Task Someone_not_set_to_show_on_the_website_is_not_listed()
    {
        await SeedAsync();

        var everyone = (await TeamAsync()).SelectMany(Names).ToList();

        Assert.Contains("ศิริพร มีสุข", everyone);
        Assert.DoesNotContain("ภานุพงศ์ ทองดี", everyone);
    }

    [Fact]
    public async Task One_department_can_be_asked_for_and_an_unknown_one_is_refused()
    {
        await SeedAsync();

        var nursing = await TeamAsync("?department=nursing");

        Assert.Equal("nursing", Assert.Single(nursing).GetProperty("department").GetString());

        using var anonymous = fixture.CreateClient();
        var response = await anonymous.GetAsync("/team?department=surgery", Token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task The_staff_list_filters_by_department_and_by_role_separately()
    {
        await SeedAsync();
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var nursing = Data(await owner.GetFromJsonAsync<JsonElement>("/staff/members?department=nursing&limit=100", Token));
        Assert.NotEmpty(nursing);
        Assert.All(nursing, member => Assert.Equal("nursing", member.GetProperty("department").GetString()));

        var vets = Data(await owner.GetFromJsonAsync<JsonElement>("/staff/members?role=veterinarian&limit=100", Token));
        Assert.Equal(5, vets.Count);
        Assert.All(vets, member => Assert.Contains(
            "veterinarian", member.GetProperty("roles").EnumerateArray().Select(role => role.GetString())));

        // A role nobody has is a role with no members; a department that does not exist is a mistake.
        var nobody = Data(await owner.GetFromJsonAsync<JsonElement>("/staff/members?role=astronaut", Token));
        Assert.Empty(nobody);

        var wrong = await owner.GetAsync("/staff/members?department=surgery", Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);
    }

    [Fact]
    public async Task The_bootstrap_owner_starts_in_management()
    {
        var bootstrapped = await FreshInstance.OwnerAsync(fixture);
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var management = Data(await owner.GetFromJsonAsync<JsonElement>("/staff/members?department=management&limit=100", Token));

        Assert.Contains(bootstrapped.StaffId, management.Select(member => member.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Departments_are_all_listed_with_how_many_people_each_has()
    {
        await SeedAsync();
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var departments = Data(await owner.GetFromJsonAsync<JsonElement>("/staff/departments", Token));

        Assert.Equal(Departments.All.Select(department => department.Code),
            departments.Select(department => department.GetProperty("department").GetString()));

        var veterinary = departments.Single(department => department.GetProperty("department").GetString() == "veterinary");
        Assert.True(veterinary.GetProperty("members").GetInt32() >= 5);
    }

    [Fact]
    public async Task Changing_a_public_profile_changes_what_the_website_says()
    {
        await SeedAsync();
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var all = Data(await owner.GetFromJsonAsync<JsonElement>("/staff/members?department=front_desk&limit=100", Token));
        var staffId = StaffIdOf(all, "panupong@" + SampleStaff.EmailDomain);

        var response = await owner.PutAsJsonAsync(
            $"/staff/members/{staffId}/public-profile",
            new { job_title = "เจ้าหน้าที่ต้อนรับภาคค่ำ", specialty = "นัดหมายนอกเวลา", career_start_year = 2022, show_on_website = true },
            Token);

        response.EnsureSuccessStatusCode();
        var profile = (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("public_profile");
        Assert.True(profile.GetProperty("show_on_website").GetBoolean());

        var frontDesk = (await TeamAsync("?department=front_desk"))[0].GetProperty("members").EnumerateArray().ToList();
        var listed = frontDesk.Single(member => member.GetProperty("id").GetString() == staffId);
        Assert.Equal("เจ้าหน้าที่ต้อนรับภาคค่ำ", listed.GetProperty("job_title").GetString());

        // A PUT replaces the whole profile: turning the listing off leaves nothing else behind.
        (await owner.PutAsJsonAsync($"/staff/members/{staffId}/public-profile", new { }, Token)).EnsureSuccessStatusCode();
        Assert.DoesNotContain("ภานุพงศ์ ทองดี", (await TeamAsync()).SelectMany(Names));
    }

    [Fact]
    public async Task A_photo_must_be_a_file_on_the_clinics_own_site()
    {
        await SeedAsync();
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var all = Data(await owner.GetFromJsonAsync<JsonElement>("/staff/members?department=finance&limit=100", Token));
        var staffId = StaffIdOf(all, "janpen@" + SampleStaff.EmailDomain);

        foreach (var outside in new[] { "https://example.com/me.jpg", "//example.com/me.jpg" })
        {
            var response = await owner.PutAsJsonAsync(
                $"/staff/members/{staffId}/public-profile", new { photo_path = outside, show_on_website = true }, Token);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Fact]
    public async Task Moving_someone_to_another_department_leaves_their_roles_alone()
    {
        await SeedAsync();
        using var owner = await FreshInstance.SignInAsOwnerAsync(fixture, Token);

        var nursing = Data(await owner.GetFromJsonAsync<JsonElement>("/staff/members?department=nursing&limit=100", Token));
        var staffId = StaffIdOf(nursing, "worawut@" + SampleStaff.EmailDomain);

        var response = await owner.PatchAsJsonAsync($"/staff/members/{staffId}", new { department = "support" }, Token);
        response.EnsureSuccessStatusCode();

        var moved = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
        Assert.Equal("support", moved.GetProperty("department").GetString());
        Assert.Equal(["vet_assistant"], moved.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));

        var unknown = await owner.PatchAsJsonAsync($"/staff/members/{staffId}", new { department = "surgery" }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
    }
}
