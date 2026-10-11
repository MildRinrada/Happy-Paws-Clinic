using System.Net;
using System.Net.Http.Json;
using HappyPaws.Domain.Access;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.RosterScenario;

namespace HappyPaws.IntegrationTests.Api.Scheduling;

/// <summary>
/// <c>GET /staff/schedules/assignment-rules</c> (<c>docs/contract-extensions.md § 48</c>): the stored
/// set comes back in the <c>PUT</c>'s own shape, and its version guards the next <c>PUT</c>.
/// </summary>
[Collection(nameof(ApiCollection))]
public class AssignmentRulesReadTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> PutAsync(HttpClient client, object body, uint? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/staff/schedules/assignment-rules") { Content = JsonContent.Create(body) };

        if (version is { } value)
        {
            request.Headers.TryAddWithoutValidation("If-Match", $"\"{value}\"");
        }

        return client.SendAsync(request, Token);
    }

    [Fact]
    public async Task The_rules_read_back_as_stored_and_their_version_guards_the_next_save()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var scheduler = fixture.Scheduler(clinic);

        try
        {
            var saved = await PutAsync(scheduler, new
            {
                rules = new object[]
                {
                    new { species = "แมว", service_id = clinic.ServiceId, staff_ids = new[] { clinic.StaffId } },
                    new { species = "cat", staff_ids = new[] { clinic.StaffId } },
                },
            });
            saved.EnsureSuccessStatusCode();
            var savedVersion = (await BodyAsync(saved, Token)).GetProperty("version").GetUInt32();

            var read = await scheduler.GetAsync("/staff/schedules/assignment-rules", Token);

            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            Assert.Equal($"\"{savedVersion}\"", read.Headers.ETag?.Tag);
            var body = await BodyAsync(read, Token);
            Assert.Equal(savedVersion, body.GetProperty("version").GetUInt32());
            var rules = body.GetProperty("rules").EnumerateArray().ToList();
            Assert.Equal(2, rules.Count);
            Assert.Equal(clinic.ServiceId, rules[0].GetProperty("service_id").GetString());
            Assert.Equal("cat", rules[1].GetProperty("species").GetString());
            Assert.Equal([clinic.StaffId], rules[1].GetProperty("staff_ids").EnumerateArray().Select(id => id.GetString()));

            var current = await PutAsync(scheduler, new { rules = new[] { new { species = "dog", staff_ids = new[] { clinic.StaffId } } } }, savedVersion);
            Assert.Equal(HttpStatusCode.OK, current.StatusCode);

            var stale = await PutAsync(scheduler, new { rules = Array.Empty<object>() }, savedVersion);
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("version_conflict", ErrorCode(await BodyAsync(stale, Token)));
        }
        finally
        {
            // Leave the rule set as other tests expect it: empty.
            (await PutAsync(scheduler, new { rules = Array.Empty<object>() })).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Reading_the_rules_needs_schedule_read_and_nothing_more()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var reader = fixture.StaffOf(clinic, Scopes.Schedule.Read);
        using var outsider = fixture.StaffOf(clinic, Scopes.Appointment.Read);

        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/staff/schedules/assignment-rules", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync("/staff/schedules/assignment-rules", Token)).StatusCode);
    }
}
