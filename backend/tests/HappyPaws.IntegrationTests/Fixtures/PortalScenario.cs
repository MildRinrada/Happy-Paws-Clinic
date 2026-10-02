using System.Net.Http.Json;
using HappyPaws.Domain.Access;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>The clinic-wide switch the owner's record page reads, set through the API.</summary>
public static class PortalScenario
{
    /// <summary>
    /// Sets which record sections owners may see. Clinic-wide, so a test that opens sections closes
    /// them again in a <c>finally</c>; the shared collection runs one test at a time.
    /// </summary>
    public static async Task OpenSectionsAsync(ApiFixture fixture, string[] sections, CancellationToken cancellationToken)
    {
        using var admin = fixture.Authenticated(TestTokens.Staff(fixture, scopes: [Scopes.Settings.Write]));

        var response = await admin.PatchAsJsonAsync(
            "/clinic/settings/medical-record", new { owner_visible_sections = sections }, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
