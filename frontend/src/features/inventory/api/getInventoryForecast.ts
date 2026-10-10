import { staffApi } from '@/lib/api/server-client';
import type { InventoryForecast } from '../types/forecast.types';

/**
 * `GET /staff/inventory/forecast`: per item at one branch, average use, days until out and a
 * suggested order. Needs `inventory:read`. docs/contract-extensions.md § 33.
 */
export function getInventoryForecast(branch: string, coverDays: number): Promise<InventoryForecast> {
  return staffApi().get<InventoryForecast>('/staff/inventory/forecast', { query: { branch, cover_days: coverDays } });
}
