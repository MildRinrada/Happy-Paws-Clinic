import { staffApi } from '@/lib/api/server-client';
import type { CrmPackage, PackageStatus } from '../types/crm.types';

/**
 * `GET /staff/packages?customer_id=`. Needs `crm:read`. One customer's packages, usable first.
 * Clinic-wide, like the loyalty account. docs/contract-extensions.md § 25.
 */
export async function listPackages(customerId: string, status?: PackageStatus): Promise<CrmPackage[]> {
  const { data } = await staffApi().get<{ data: CrmPackage[] }>('/staff/packages', {
    query: { customer_id: customerId, status },
  });

  return data;
}
