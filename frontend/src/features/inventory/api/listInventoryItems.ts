import { staffApi } from '@/lib/api/server-client';
import type { InventoryItem, ItemsQuery } from '../types/inventory.types';

/**
 * `GET /staff/inventory/items`: "ยาและเวชภัณฑ์, คงเหลือรายสาขา". Needs `inventory:read`. The whole
 * catalogue by name, each item with its stock at the branch asked for, or at every branch the
 * caller may see.
 */
export async function listInventoryItems(query: ItemsQuery = {}): Promise<InventoryItem[]> {
  const { data } = await staffApi().get<{ data: InventoryItem[] }>('/staff/inventory/items', {
    query: { q: query.q || undefined, status: query.status || undefined, branch: query.branch || undefined },
  });

  return data;
}
