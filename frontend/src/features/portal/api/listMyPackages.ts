import { staffApi } from '@/lib/api/server-client';
import type { PackageBalance } from '../types/package.types';

/**
 * `GET /portal/packages` — "สิทธิ์คงเหลือของลูกค้าและวันหมดอายุ". Usable ones first, soonest to
 * expire first, then the finished ones. A package sold at the counter shows as `pending_payment`
 * until its bill is paid.
 */
export async function listMyPackages(): Promise<PackageBalance[]> {
  const { data } = await staffApi().get<{ data: PackageBalance[] }>('/portal/packages');
  return data;
}
