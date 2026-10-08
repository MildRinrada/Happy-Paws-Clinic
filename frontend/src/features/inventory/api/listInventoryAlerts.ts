import { staffApi } from '@/lib/api/server-client';
import type { InventoryAlert } from '../types/inventory.types';

/**
 * `GET /staff/inventory/alerts`: "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน", worst first. Needs
 * `inventory:read`. `within_days` is how far ahead "ใกล้หมดอายุ" looks (90 when absent).
 */
export async function listInventoryAlerts(branch?: string, withinDays?: number): Promise<InventoryAlert[]> {
  const { data } = await staffApi().get<{ data: InventoryAlert[] }>('/staff/inventory/alerts', {
    query: { branch, within_days: withinDays },
  });

  return data;
}
