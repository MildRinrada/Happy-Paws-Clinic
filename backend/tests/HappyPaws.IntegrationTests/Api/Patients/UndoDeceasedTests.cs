using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Domain.Access;
using HappyPaws.Domain.Appointments;
using HappyPaws.IntegrationTests.Fixtures;
using static HappyPaws.IntegrationTests.Fixtures.BookingScenario;

namespace HappyPaws.IntegrationTests.Api.Patients;

/// <summary>
/// <c>POST /staff/pets/{id}/deceased/undo</c>: taking back a death recorded by mistake. Not in the
/// API reference; see <c>docs/contract-extensions.md § 29</c>. The reminder side is in
/// <c>UndoDeceasedReminderTests</c>, on the scheduler's own clinic.
/// </summary>
[Collection(nameof(ApiCollection))]
public class UndoDeceasedTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private HttpClient Counter(SeededClinic clinic, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            clinic.StaffUserId,
            clinic.StaffId,
            scopes.Length == 0 ? [Scopes.Patient.Read, Scopes.Patient.Write, Scopes.Audit.Read] : scopes,
            [clinic.BranchId]));

    private static Task<HttpResponseMessage> UndoAsync(HttpClient client, string petId, string? reason) =>
        client.PostAsJsonAsync($"/staff/pets/{petId}/deceased/undo", new { reason }, Token);

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(Token);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>Null fields are left out of the answer, so absent and null read the same.</summary>
    private static bool IsNull(JsonElement element, string name) =>
        !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null;

    private static async Task<JsonElement> ErrorAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("error");

    private async Task MarkDeceasedAsync(HttpClient counter, string petId, object? body = null) =>
        (await counter.PostAsJsonAsync($"/staff/pets/{petId}/deceased", body ?? new { }, Token)).EnsureSuccessStatusCode();

    [Fact]
    public async Task It_takes_patient_write()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        await MarkDeceasedAsync(counter, clinic.PetId);

        using var reader = Counter(clinic, Scopes.Patient.Read);
        var response = await UndoAsync(reader, clinic.PetId, "บันทึกผิดตัว");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task A_reason_is_required(string? reason)
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        await MarkDeceasedAsync(counter, clinic.PetId);

        var response = await UndoAsync(counter, clinic.PetId, reason);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("reason", (await ErrorAsync(response)).GetProperty("field").GetString());

        var pet = await PatientSeed.ReloadPetAsync(fixture, clinic.PetId, Token);
        Assert.True(pet!.IsDeceased);
    }

    [Fact]
    public async Task A_living_patient_has_no_death_to_undo()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);

        var response = await UndoAsync(counter, clinic.PetId, "กดผิด");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("state_conflict", (await ErrorAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Undoing_opens_the_record_again_and_the_patient_can_be_booked()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var counter = Counter(clinic);
        var died = DateTimeOffset.UtcNow.AddDays(-1);
        await MarkDeceasedAsync(counter, clinic.PetId, new { occurred_at = died });

        var result = await OkAsync(await UndoAsync(counter, clinic.PetId, "บันทึกผิดตัว ตัวที่เสียชีวิตคือพี่น้องในบ้านเดียวกัน"));

        Assert.Equal(clinic.PetId, result.GetProperty("pet_id").GetString());
        Assert.Equal(
            died.ToUnixTimeMilliseconds(),
            result.GetProperty("previous_deceased_at").GetDateTimeOffset().ToUnixTimeMilliseconds());

        var pet = (await counter.GetFromJsonAsync<JsonElement>($"/staff/pets/{clinic.PetId}", Token)).GetProperty("pet");
        Assert.False(pet.GetProperty("is_deceased").GetBoolean());
        Assert.True(IsNull(pet, "deceased_at"));

        await BookAsStaffAsync(fixture, clinic, 6, Token);
    }

    [Fact]
    public async Task The_visits_the_death_cancelled_stay_cancelled_and_are_listed_for_rebooking()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var soon = await BookAsStaffAsync(fixture, clinic, 4, Token);
        var later = await BookAsStaffAsync(fixture, clinic, 8, Token);

        using var counter = Counter(clinic);
        await MarkDeceasedAsync(counter, clinic.PetId);

        var result = await OkAsync(await UndoAsync(counter, clinic.PetId, "สัตว์ยังมีชีวิต เจ้าของโทรมาแจ้ง"));
        var listed = result.GetProperty("cancelled_appointments").EnumerateArray().ToList();

        Assert.Equal([soon, later], listed.Select(row => row.GetProperty("id").GetString()).ToList());
        Assert.All(listed, row => Assert.False(row.GetProperty("has_passed").GetBoolean()));
        Assert.Equal(clinic.BranchId, listed[0].GetProperty("branch_id").GetString());

        // Not rebooked: their slots may have gone to someone else, and the owner decides.
        foreach (var id in new[] { soon, later })
        {
            var appointment = await ClinicSeed.ReloadAppointmentAsync(fixture, id, Token);
            Assert.Equal(AppointmentStatus.Cancelled, appointment!.Status);
        }

        var all = await ClinicSeed.AppointmentsAtAsync(fixture, clinic.BranchId, Token);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task A_second_death_after_an_undo_lists_only_its_own_cancellations()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var first = await BookAsStaffAsync(fixture, clinic, 4, Token);

        using var counter = Counter(clinic);
        await MarkDeceasedAsync(counter, clinic.PetId);
        await OkAsync(await UndoAsync(counter, clinic.PetId, "กดผิดตัว"));

        var second = await BookAsStaffAsync(fixture, clinic, 10, Token);
        await MarkDeceasedAsync(counter, clinic.PetId);

        var result = await OkAsync(await UndoAsync(counter, clinic.PetId, "กดผิดอีกครั้ง"));
        var ids = result.GetProperty("cancelled_appointments").EnumerateArray()
            .Select(row => row.GetProperty("id").GetString()).ToList();

        Assert.Equal([second], ids);
        Assert.DoesNotContain(first, ids);
    }

    [Fact]
    public async Task The_undo_is_audited_with_the_reason_and_the_date_of_death_it_took_back()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var booked = await BookAsStaffAsync(fixture, clinic, 4, Token);

        using var counter = Counter(clinic);
        await MarkDeceasedAsync(counter, clinic.PetId);
        await OkAsync(await UndoAsync(counter, clinic.PetId, "บันทึกผิดตัว"));

        var log = await counter.GetFromJsonAsync<JsonElement>($"/staff/audit-logs?pet_id={clinic.PetId}", Token);
        var actions = log.GetProperty("data").EnumerateArray().Select(row => row.GetProperty("action").GetString()).ToList();

        // The death's own row stays: the undo is a second event, not an erasure of the first.
        Assert.Contains("pet.marked_deceased", actions);

        var undo = log.GetProperty("data").EnumerateArray()
            .Single(row => row.GetProperty("action").GetString() == "pet.deceased_undone");
        var details = undo.GetProperty("details");

        Assert.Equal("บันทึกผิดตัว", details.GetProperty("reason").GetString());
        Assert.Equal(JsonValueKind.String, details.GetProperty("previous_deceased_at").ValueKind);
        Assert.Equal([booked], details.GetProperty("cancelled_appointment_ids").EnumerateArray().Select(id => id.GetString()).ToList());
    }

    [Fact]
    public async Task The_owner_is_not_messaged_again()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        await BookAsStaffAsync(fixture, clinic, 4, Token);

        using var counter = Counter(clinic);
        await MarkDeceasedAsync(counter, clinic.PetId);

        using var owner = fixture.CustomerOf(clinic);
        var before = (await owner.GetFromJsonAsync<JsonElement>("/portal/notifications", Token)).GetProperty("data").GetArrayLength();

        await OkAsync(await UndoAsync(counter, clinic.PetId, "บันทึกผิดตัว"));

        var after = (await owner.GetFromJsonAsync<JsonElement>("/portal/notifications", Token)).GetProperty("data").GetArrayLength();
        Assert.Equal(before, after);
    }
}
