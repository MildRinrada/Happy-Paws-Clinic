import { listServices } from '@/features/service-catalog/api/listServices';
import { listManagedServices } from '@/features/service-catalog/api/manage-services';
import type { ServiceOption } from '../types/crm.types';

/**
 * The services a package or a coupon can cover, the same way `/billing/new` finds them: every
 * active service under `service:manage`, otherwise the public menu. A failed read gives an empty
 * list rather than costing the page.
 */
export async function loadServiceOptions(branchId: string | undefined, canManageServices: boolean): Promise<ServiceOption[]> {
  if (canManageServices) {
    const managed = await listManagedServices().catch(() => null);
    if (managed) {
      return managed
        .filter((service) => service.is_active && (!branchId || service.branch_ids.length === 0 || service.branch_ids.includes(branchId)))
        .map(({ id, name }) => ({ id, name }));
    }
  }

  return (await listServices(branchId).catch(() => [])).map(({ id, name }) => ({ id, name }));
}
