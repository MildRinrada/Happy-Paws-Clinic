using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.ServiceCatalog.Dtos;
using HappyPaws.Domain.ServiceCatalog;

namespace HappyPaws.Application.ServiceCatalog.ListBookableServices;

/// <summary><c>GET /services</c> — "เฉพาะบริการที่เปิดให้จองออนไลน์".</summary>
/// <param name="CategoryCode">Narrow to one heading of the menu.</param>
/// <param name="BranchId">Narrow to what one branch offers.</param>
public sealed record ListBookableServicesQuery(string? CategoryCode = null, string? BranchId = null);

/// <summary>
/// The public service menu.
/// <para>
/// Public, and filtered accordingly: only services the clinic has opened for online booking. The
/// internal theatre list is not enumerable from here, which is the same line
/// <c>GET /booking/availability</c> draws when it answers <c>404</c> for a service that exists but
/// is not offered online.
/// </para>
/// <para>
/// Prices are the ones in force today, in clinic time. A price list dated next month is not shown
/// early — a customer quoted next month's price for tomorrow's visit has been quoted wrongly.
/// </para>
/// </summary>
public sealed class ListBookableServicesHandler(IServiceRepository services, IClock clock)
{
    public async Task<IReadOnlyList<PublicServiceDto>> HandleAsync(
        ListBookableServicesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CategoryCode is { Length: > 0 } category && !ServiceCategories.IsKnown(category))
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["category"] = ["ไม่รู้จักหมวดบริการนี้"] });
        }

        var today = DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);
        var bookable = await services.ListBookableAsync(query.CategoryCode, query.BranchId, cancellationToken);

        return [.. bookable.Select(service => PublicServiceDto.From(service, today))];
    }
}

/// <summary>
/// <c>GET /services/categories</c> — "ตรวจ วัคซีน ทันตกรรม ผ่าตัด แล็บ กรูมมิ่ง". The contract's
/// six, in the contract's order.
/// </summary>
public sealed class ListServiceCategoriesHandler
{
    public IReadOnlyList<ServiceCategoryDto> Handle() =>
        [.. ServiceCategories.All.Select(ServiceCategoryDto.From)];
}
