using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Crm;
using HappyPaws.Infrastructure.Persistence;
using HappyPaws.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static HappyPaws.IntegrationTests.Fixtures.ConsultationScenario;

namespace HappyPaws.IntegrationTests.Api.Portal;

/// <summary>The customer portal: pets, the owner's edits, the records page, the inbox, packages and home.</summary>
[Collection(nameof(ApiCollection))]
public class PortalTests(ApiFixture fixture)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_owner_sees_their_pets_and_changes_only_the_owners_fields()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var customer = fixture.CustomerOf(clinic);

        var response = await customer.PatchAsJsonAsync(
            $"/portal/pets/{clinic.PetId}",
            new { nickname = "ลูลู่", diet = "อาหารเม็ดสูตรผิวแพ้ง่าย", behaviour = "กลัวเสียงไดร์", name = "ชื่อใหม่", species = "แมว" },
            Token);

        response.EnsureSuccessStatusCode();
        var pet = await response.Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal("ลูลู่", pet.GetProperty("nickname").GetString());
        Assert.Equal("กลัวเสียงไดร์", pet.GetProperty("behaviour").GetString());

        // Fields the owner has no business changing are not in the request type at all.
        Assert.Equal("ลูน่า", pet.GetProperty("name").GetString());
        Assert.Equal("สุนัข", pet.GetProperty("species").GetString());

        var list = await customer.GetFromJsonAsync<JsonElement>("/portal/pets", Token);
        Assert.Equal("ลูลู่", list.GetProperty("data")[0].GetProperty("nickname").GetString());
    }

    [Fact]
    public async Task Someone_elses_pet_is_not_found()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var (_, otherPet) = await ClinicSeed.OtherOwnerAsync(fixture, Token);
        using var customer = fixture.CustomerOf(clinic);

        var patch = await customer.PatchAsJsonAsync($"/portal/pets/{otherPet}", new { nickname = "x" }, Token);
        var records = await customer.GetAsync($"/portal/pets/{otherPet}/records", Token);

        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, records.StatusCode);
    }

    [Fact]
    public async Task A_photo_must_be_an_uploaded_image_of_the_same_pet()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var customer = fixture.CustomerOf(clinic);

        var started = await (await customer.PostAsJsonAsync(
            "/portal/documents",
            new { pet_id = clinic.PetId, file_name = "luna.png", content_type = "image/png", size_bytes = 8, kind = "photo" },
            Token)).Content.ReadFromJsonAsync<JsonElement>(Token);

        var documentId = started.GetProperty("document").GetProperty("id").GetString();

        var early = await customer.PatchAsJsonAsync($"/portal/pets/{clinic.PetId}", new { photo_document_id = documentId }, Token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, early.StatusCode);

        using var storage = fixture.CreateClient();
        using var bytes = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4]);
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        (await storage.PutAsync(new Uri(started.GetProperty("upload").GetProperty("url").GetString()!).PathAndQuery, bytes, Token))
            .EnsureSuccessStatusCode();

        var set = await customer.PatchAsJsonAsync($"/portal/pets/{clinic.PetId}", new { photo_document_id = documentId }, Token);
        set.EnsureSuccessStatusCode();

        Assert.Contains("/storage/files/", (await set.Content.ReadFromJsonAsync<JsonElement>(Token)).GetProperty("photo_url").GetString());
    }

    [Fact]
    public async Task The_records_page_shows_only_the_sections_the_clinic_opened()
    {
        var open = await OpenCaseAsync(fixture, Token);
        using var vet = fixture.Vet(open.Clinic);

        await DiagnoseAsync(vet, open.ConsultationId, Token, "หูอักเสบ");
        await CloseAsync(vet, open.ConsultationId, Token);

        using var customer = fixture.CustomerOf(open.Clinic);

        var closed = await customer.GetFromJsonAsync<JsonElement>($"/portal/pets/{open.Clinic.PetId}/records", Token);
        var closedVisit = closed.GetProperty("visits")[0];

        // With nothing opened, the owner sees that a visit happened and nothing of what was found.
        Assert.Equal(0, closed.GetProperty("visible_sections").GetArrayLength());
        Assert.False(closedVisit.TryGetProperty("diagnoses", out _));

        await PortalScenario.OpenSectionsAsync(fixture, ["diagnosis"], Token);

        try
        {
            var opened = await customer.GetFromJsonAsync<JsonElement>($"/portal/pets/{open.Clinic.PetId}/records", Token);
            var visit = opened.GetProperty("visits")[0];

            Assert.Equal("หูอักเสบ", visit.GetProperty("diagnoses")[0].GetString());
            Assert.False(visit.TryGetProperty("treatments", out _));
        }
        finally
        {
            await PortalScenario.OpenSectionsAsync(fixture, [], Token);
        }
    }

    [Fact]
    public async Task Messages_the_clinic_sends_land_in_the_inbox_and_can_be_marked_read()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using (var scope = fixture.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<INotificationDispatcher>().SendAsync(
                new NotificationRequest(clinic.CustomerId, NotificationTemplates.QueueCalled, new Dictionary<string, string> { ["number"] = "A014", ["room"] = "room_2" }),
                Token);
        }

        using var customer = fixture.CustomerOf(clinic);

        var inbox = await customer.GetFromJsonAsync<JsonElement>("/portal/notifications", Token);
        Assert.Equal(1, inbox.GetProperty("unread_count").GetInt32());
        Assert.Equal("ถึงคิว A014 แล้ว", inbox.GetProperty("data")[0].GetProperty("title").GetString());

        var marked = await (await customer.PostAsJsonAsync("/portal/notifications/read", new { }, Token))
            .Content.ReadFromJsonAsync<JsonElement>(Token);

        Assert.Equal(1, marked.GetProperty("marked").GetInt32());
        Assert.Equal(0, marked.GetProperty("unread_count").GetInt32());
    }

    [Fact]
    public async Task Packages_show_what_is_left_and_when_it_runs_out()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);

        using (var scope = fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
            var package = CustomerPackage.Sell(clinic.CustomerId, "ตรวจสุขภาพ 4 ครั้ง", [clinic.ServiceId], 4, DateTimeOffset.UtcNow, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)));
            package.Use(DateOnly.FromDateTime(DateTime.UtcNow));
            db.Set<CustomerPackage>().Add(package);
            await db.SaveChangesAsync(Token);
        }

        using var customer = fixture.CustomerOf(clinic);
        var packages = await customer.GetFromJsonAsync<JsonElement>("/portal/packages", Token);
        var first = packages.GetProperty("data")[0];

        Assert.Equal(3, first.GetProperty("remaining").GetInt32());
        Assert.Equal("active", first.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Home_brings_the_pets_the_next_visit_and_the_inbox_together()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var booked = await BookingScenario.BookAsStaffAsync(fixture, clinic, 5, Token);
        using var customer = fixture.CustomerOf(clinic);

        var home = await customer.GetFromJsonAsync<JsonElement>("/portal/home", Token);

        Assert.Equal(clinic.CustomerId, home.GetProperty("customer_id").GetString());
        Assert.Equal(clinic.PetId, home.GetProperty("pets")[0].GetProperty("id").GetString());
        Assert.Equal(booked, home.GetProperty("next_appointment").GetProperty("id").GetString());
        Assert.Equal(0, home.GetProperty("queue").GetArrayLength());
    }

    [Fact]
    public async Task Home_says_whose_ticket_each_queue_number_is()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        var booked = await BookingScenario.BookAsStaffAsync(fixture, clinic, 3, Token);
        using var vet = fixture.Vet(clinic);
        var ticketId = await CheckInAsync(vet, booked, Token);
        using var customer = fixture.CustomerOf(clinic);

        var home = await customer.GetFromJsonAsync<JsonElement>("/portal/home", Token);
        var ticket = home.GetProperty("queue")[0];

        // Without these an owner with two pets in on one day could not tell which number is whose.
        Assert.Equal(ticketId, ticket.GetProperty("ticket_id").GetString());
        Assert.Equal(clinic.PetId, ticket.GetProperty("pet_id").GetString());
        Assert.Equal(booked, ticket.GetProperty("appointment_id").GetString());
    }

    [Fact]
    public async Task Staff_tokens_cannot_use_the_portal()
    {
        var clinic = await ClinicSeed.ClinicAsync(fixture, cancellationToken: Token);
        using var staff = fixture.StaffOf(clinic);

        var response = await staff.GetAsync("/portal/home", Token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
