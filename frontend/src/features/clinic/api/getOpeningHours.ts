import { publicApi } from '@/lib/api/server-client';
import type { OpeningHours } from '../types/clinic.types';

/** `GET /clinic/hours` — public. The weekly hours and upcoming special closures. */
export function getOpeningHours(): Promise<OpeningHours> {
  return publicApi().get<OpeningHours>('/clinic/hours');
}
