using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Integrations;

/// <summary>
/// Finds the customer a LINE address is registered to.
/// <para>
/// An address lookup, the way a mail system attributes a reply to the address it was sent to: it
/// identifies nobody, grants no session, and only finds a customer whose record already carries
/// that address. See <c>docs/assumptions.md § B</c>.
/// </para>
/// </summary>
public interface ILineAddressBook
{
    /// <summary>The live customer with this LINE address, tracked for an update, or null.</summary>
    Task<Customer?> FindTrackedAsync(string lineUserId, CancellationToken cancellationToken = default);
}
