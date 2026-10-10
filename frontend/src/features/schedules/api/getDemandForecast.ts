import { staffApi } from '@/lib/api/server-client';
import type { DemandForecast } from '../types/forecast.types';

/**
 * `GET /staff/stats/demand-forecast`: per future day and hour, bookings, walk-ins expected, vets on
 * shift and whether the load per vet is high. Needs `stats:read`. Today or later, at most 14 days.
 * docs/contract-extensions.md § 33.
 */
export function getDemandForecast(branch: string, from: string, to: string): Promise<DemandForecast> {
  return staffApi().get<DemandForecast>('/staff/stats/demand-forecast', { query: { branch, from, to } });
}
