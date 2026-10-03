import { staffApi } from '@/lib/api/server-client';
import type { PortalHome } from '../types/portal.types';

/** `GET /portal/home` — the owner's pets, next appointment, today's queue and inbox, in one call. */
export function getPortalHome(): Promise<PortalHome> {
  return staffApi().get<PortalHome>('/portal/home');
}
