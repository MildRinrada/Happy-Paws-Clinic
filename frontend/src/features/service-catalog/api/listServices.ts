import { publicApi } from '@/lib/api/server-client';
import type { PublicService } from '../types/service.types';

/**
 * `GET /services?branch=` — the public menu, narrowed to what one branch offers.
 *
 * This is the only service list the contract has, and it holds only services opened for online
 * booking. A service the clinic takes by phone alone is therefore not in it, and the counter
 * cannot pick one here. That is a gap in the contract, recorded in docs/assumptions.md § 27 —
 * not something this module may paper over by inventing a staff endpoint.
 */
export async function listServices(branchId?: string): Promise<PublicService[]> {
  const { data } = await publicApi().get<{ data: PublicService[] }>('/services', {
    query: { branch: branchId },
  });

  return data;
}
