using HappyPaws.Domain.Access;
using HappyPaws.Domain.Billing;
using HappyPaws.Domain.Consultations;
using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Reviews;
using HappyPaws.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.IntegrationTests.Fixtures;

/// <summary>
/// Visits, invitations and the clients that review them (<c>docs/contract-extensions.md § 36</c>).
/// Visits are written straight to the tables, through the production factories, at a clinic day
/// and time the test chooses.
/// </summary>
public static class ReviewScenario
{
    public static DateOnly Today => AutomationScenario.ClinicToday;

    /// <summary>The seeded customer, signed in to the portal.</summary>
    public static HttpClient Portal(this ApiFixture fixture, SeededClinic clinic) =>
        fixture.Authenticated(TestTokens.Customer(fixture, clinic.CustomerUserId, clinic.CustomerId));

    /// <summary>Another customer, signed in to the portal.</summary>
    public static HttpClient PortalAs(this ApiFixture fixture, string customerId) =>
        fixture.Authenticated(TestTokens.Customer(fixture, $"usr_{customerId}", customerId));

    /// <summary>A member of staff with <c>content:manage</c>, or the scopes given.</summary>
    public static HttpClient Moderator(this ApiFixture fixture, string staffId, params string[] scopes) =>
        fixture.Authenticated(TestTokens.Staff(
            fixture,
            $"usr_{staffId}",
            staffId,
            scopes.Length == 0 ? [Scopes.Content.Manage] : scopes));

    /// <summary>A bill for a pet on a clinic day <paramref name="daysAgo"/> days back: a visit, unless voided.</summary>
    public static async Task<string> BillAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        int daysAgo,
        CancellationToken cancellationToken,
        string? customerId = null,
        string? petId = null,
        bool withPet = true,
        bool voided = false,
        int hour = 11)
    {
        var at = AutomationScenario.ClinicTime(Today.AddDays(-daysAgo), hour);
        var invoice = Invoice.Raise(
            $"R-{Guid.NewGuid():N}"[..20],
            clinic.BranchId,
            customerId ?? clinic.CustomerId,
            clinic.StaffId,
            at,
            withPet ? petId ?? clinic.PetId : null);

        if (voided)
        {
            invoice.Void("ออกผิด", clinic.StaffId, at.AddMinutes(5));
        }

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }

    /// <summary>A consultation on a clinic day <paramref name="daysAgo"/> days back, closed or still open.</summary>
    public static async Task<string> ConsultationAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        int daysAgo,
        bool closed,
        CancellationToken cancellationToken,
        int hour = 10)
    {
        var at = AutomationScenario.ClinicTime(Today.AddDays(-daysAgo), hour);
        var consultation = Consultation.Open(clinic.BranchId, clinic.PetId, clinic.CustomerId, clinic.StaffId, at);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<Consultation>().Add(consultation);
        await db.SaveChangesAsync(cancellationToken);

        if (closed)
        {
            // Closing through the domain needs a diagnosis and a signed record; the visit rule only
            // reads the status, so the test sets it.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE consultations SET status = 'Closed', closed_at = {at.AddMinutes(30).ToUniversalTime()} WHERE id = {consultation.Id}",
                cancellationToken);
        }

        return consultation.Id;
    }

    /// <summary>Renames a customer, for the tests about which name a review shows.</summary>
    public static async Task RenameAsync(ApiFixture fixture, string customerId, string displayName, CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE customers SET display_name = {displayName} WHERE id = {customerId}",
            cancellationToken);
    }

    /// <summary>Records a pet as deceased, straight in the table.</summary>
    public static async Task MarkDeceasedAsync(ApiFixture fixture, string petId, CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE pets SET deceased_at = {DateTimeOffset.UtcNow} WHERE id = {petId}",
            cancellationToken);
    }

    /// <summary>Gives the customer a portal account, so a review message links to the portal.</summary>
    public static async Task LinkAccountAsync(ApiFixture fixture, string customerId, CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var customer = await db.Set<Customer>().SingleAsync(row => row.Id == customerId, cancellationToken);
        customer.LinkAccount($"usr_acct_{Guid.NewGuid():N}"[..30]);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>An invitation with a token for a visit, made <paramref name="madeDaysAgo"/> days back. Returns the token.</summary>
    public static async Task<string> InvitationAsync(
        ApiFixture fixture,
        SeededClinic clinic,
        string visitId,
        int visitDaysAgo,
        CancellationToken cancellationToken,
        int madeDaysAgo = 0)
    {
        var (invitation, token) = ReviewInvitation.Create(
            clinic.CustomerId,
            clinic.BranchId,
            Today.AddDays(-visitDaysAgo),
            visitId,
            clinic.PetId,
            "ตรวจสุขภาพ",
            withToken: true,
            dispatchId: null,
            DateTimeOffset.UtcNow.AddDays(-madeDaysAgo));

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        db.Set<ReviewInvitation>().Add(invitation);
        await db.SaveChangesAsync(cancellationToken);

        return token!;
    }

    /// <summary>Every invitation of a customer, as stored.</summary>
    public static async Task<List<ReviewInvitation>> InvitationsOfAsync(ApiFixture fixture, string customerId, CancellationToken cancellationToken)
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();

        return await db.Set<ReviewInvitation>().AsNoTracking()
            .Where(invitation => invitation.CustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>A review body.</summary>
    public static object Body(int rating = 5, string? text = "หมอใจดี อธิบายละเอียด", string displayName = "first_name", string? visitId = null) =>
        visitId is null
            ? new { rating, text, display_name = displayName }
            : new { visit_id = visitId, rating, text, display_name = displayName };
}
