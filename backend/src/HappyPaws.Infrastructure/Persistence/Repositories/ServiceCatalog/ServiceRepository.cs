using HappyPaws.Application.ServiceCatalog;
using HappyPaws.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.ServiceCatalog;

/// <summary>EF Core implementation of the service-catalogue port.</summary>
internal sealed class ServiceRepository(HappyPawsDbContext db) : IServiceRepository
{
    public void Add(ClinicService service) => db.Set<ClinicService>().Add(service);

    public Task<ClinicService?> FindTrackedByIdAsync(string serviceId, CancellationToken cancellationToken = default) =>
        db.Set<ClinicService>().FirstOrDefaultAsync(service => service.Id == serviceId, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        db.Set<ClinicService>().AnyAsync(service => service.Code == code, cancellationToken);

    public async Task<IReadOnlyList<ClinicService>> ListBookableAsync(
        string? categoryCode = null,
        string? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var services = db.Set<ClinicService>().AsNoTracking()
            .Where(service => service.IsActive && service.OnlineBookingEnabled);

        if (categoryCode is { Length: > 0 })
        {
            services = services.Where(service => service.CategoryCode == categoryCode);
        }

        if (branchId is { Length: > 0 })
        {
            // An empty list means "every branch", so it has to pass the filter rather than fail it.
            services = services.Where(
                service => service.BranchIds.Count == 0 || service.BranchIds.Contains(branchId));
        }

        // Grouped the way a menu reads: by heading, then by name.
        return await services
            .OrderBy(service => service.CategoryCode).ThenBy(service => service.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicService>> ListAllAsync(
        string? categoryCode = null,
        CancellationToken cancellationToken = default)
    {
        var services = db.Set<ClinicService>().AsNoTracking();

        if (categoryCode is { Length: > 0 })
        {
            services = services.Where(service => service.CategoryCode == categoryCode);
        }

        return await services
            .OrderBy(service => service.CategoryCode).ThenBy(service => service.Name)
            .ToListAsync(cancellationToken);
    }
}
