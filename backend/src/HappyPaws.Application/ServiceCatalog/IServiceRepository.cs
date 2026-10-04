using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.ServiceCatalog;

/// <summary>
/// Persistence port for the service catalogue.
/// <para>
/// Booking reads services through <c>IBookingCatalog</c>, which is the booking-shaped view of four
/// modules' data. This is the catalogue's own port: the public menu and the writes behind
/// <c>service:manage</c>.
/// </para>
/// </summary>
public interface IServiceRepository
{
    void Add(ClinicService service);

    /// <summary>Tracked, for the handlers that change a service.</summary>
    Task<ClinicService?> FindTrackedByIdAsync(string serviceId, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /services</c> — "เฉพาะบริการที่เปิดให้จองออนไลน์". Active, open for online booking,
    /// optionally narrowed to a category or to the services a branch offers.
    /// </summary>
    Task<IReadOnlyList<ClinicService>> ListBookableAsync(
        string? categoryCode = null,
        string? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET /staff/services</c> (an extension, <c>docs/contract-extensions.md § 17</c>): every
    /// service, counter-only and inactive ones included, optionally narrowed to one category.
    /// </summary>
    Task<IReadOnlyList<ClinicService>> ListAllAsync(
        string? categoryCode = null,
        CancellationToken cancellationToken = default);
}
