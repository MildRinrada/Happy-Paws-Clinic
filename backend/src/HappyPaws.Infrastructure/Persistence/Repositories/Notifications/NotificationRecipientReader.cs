using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.Notifications;

/// <summary>
/// Where a customer can be reached, and on which channels they still want to be.
/// <c>Allow*</c> are the choices on their portal account (<c>PATCH /me</c>, "การแจ้งเตือน");
/// a customer with no account has made none, so every channel is allowed.
/// </summary>
internal sealed record NotificationRecipient(
    string CustomerId,
    string Phone,
    string? Email,
    string? LineUserId,
    bool LineReachable,
    bool AllowLine,
    bool AllowSms,
    bool AllowEmail);

/// <summary>Reads a <see cref="NotificationRecipient"/>: the customer record and, if any, its portal account.</summary>
internal sealed class NotificationRecipientReader(HappyPawsDbContext db)
{
    public async Task<NotificationRecipient?> ReadAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await db.Set<Customer>().AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == customerId, cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var account = customer.UserId is { } userId
            ? await db.Set<UserAccount>().AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            : null;

        return new NotificationRecipient(
            customer.Id,
            customer.Phone,
            string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email,
            customer.LineUserId,
            customer.CanReceiveLine,
            account?.NotifyByLine ?? true,
            account?.NotifyBySms ?? true,
            account?.NotifyByEmail ?? true);
    }
}
