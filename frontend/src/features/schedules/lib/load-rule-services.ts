import { listServices } from '@/features/service-catalog/api/listServices';
import { listManagedServices } from '@/features/service-catalog/api/manage-services';

interface RuleService {
  id: string;
  name: string;
}

/**
 * The services an assignment rule can name. With `service:manage` that is every active service,
 * counter-only ones included (`GET /staff/services`); without it, only the public menu of the
 * branch in view, the one service list the contract gives everyone. A failed read leaves the list
 * empty rather than costing the roster its page: rules by species alone still work.
 */
export async function loadRuleServices(branchId: string, canManageServices: boolean): Promise<RuleService[]> {
  if (canManageServices) {
    const managed = await listManagedServices().catch(() => null);
    if (managed) {
      return managed.filter((service) => service.is_active).map(({ id, name }) => ({ id, name }));
    }
  }

  const offered = await listServices(branchId).catch(() => []);
  return offered.map(({ id, name }) => ({ id, name }));
}
