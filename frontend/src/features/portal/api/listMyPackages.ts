import { staffApi } from '@/lib/api/server-client';
import type { PackageBalance } from '../types/package.types';

/**
 * `GET /portal/packages` — "สิทธิ์คงเหลือของลูกค้าและวันหมดอายุ". Usable ones first, soonest to
 * expire first, then the finished ones. Empty until the clinic sells packages, which is not built.
 */
export async function listMyPackages(): Promise<PackageBalance[]> {
  const { data } = await staffApi().get<{ data: PackageBalance[] }>('/portal/packages');
  return data;
}
