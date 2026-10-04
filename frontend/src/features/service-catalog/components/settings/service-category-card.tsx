import { Card, CardHeader } from '@/components/ui/card';
import { serviceSettings } from '../../content/service-settings';
import type { ManagedService, ServiceCategory } from '../../types/managed-service.types';
import { ServiceRow, type ServiceRowProps } from './service-row';

interface ServiceCategoryCardProps extends Omit<ServiceRowProps, 'service'> {
  category: ServiceCategory;
  services: ManagedService[];
}

/** One heading of the catalogue and its services, on sale first. */
export function ServiceCategoryCard({ category, services, ...shared }: ServiceCategoryCardProps) {
  const ordered = [...services].sort((a, b) => Number(b.is_active) - Number(a.is_active) || a.name.localeCompare(b.name, 'th'));

  return (
    <Card>
      <CardHeader title={category.name} aside={<span className="text-sm text-ink-500">{services.length}</span>} />
      {ordered.length === 0 ? (
        <p className="px-5 py-4 text-sm text-ink-500">{serviceSettings.empty}</p>
      ) : (
        <ul className="divide-y divide-ink-100">
          {ordered.map((service) => (
            // Keyed by version: a save reloads the page, and the row starts fresh from what was stored.
            <ServiceRow key={`${service.id}:${service.version}`} service={service} {...shared} />
          ))}
        </ul>
      )}
    </Card>
  );
}
