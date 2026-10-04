using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.ServiceCatalog.Dtos;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.ServiceCatalog.ListServices;

/// <summary><c>GET /staff/services</c>. Not in the API reference; see <c>docs/contract-extensions.md § 17</c>.</summary>
/// <param name="CategoryCode">Narrow to one heading.</param>
public sealed record ListServicesQuery(string? CategoryCode = null);

/// <summary>
/// The whole catalogue as the people who manage it see it.
/// <para>
/// The public menu (<c>GET /services</c>) shows only what is open for online booking, so a service
/// booked at the counter only, or one taken off sale, cannot be found there to be edited. This
/// list has both, each with its version for <c>If-Match</c> and its upcoming prices.
/// </para>
/// </summary>
public sealed class ListServicesHandler(IServiceRepository services, IClock clock)
{
    public async Task<IReadOnlyList<ServiceDto>> HandleAsync(
        ListServicesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CategoryCode is { Length: > 0 } category && !ServiceCategories.IsKnown(category))
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["category"] = ["ไม่รู้จักหมวดบริการนี้"] });
        }

        var today = DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);
        var all = await services.ListAllAsync(query.CategoryCode, cancellationToken);

        return [.. all.Select(service => ServiceDto.From(service, today))];
    }
}
